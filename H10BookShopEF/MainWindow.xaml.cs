using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; //for ObservableCollection
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace H10BookShopEF
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    BookShopEntities ctx;
    ObservableCollection<Book> localBooks;
    ICollectionView view; //filtteröintiä varten
    bool IsBooks;
    public MainWindow()
    {
      InitializeComponent();
      IniMyStuff();
    }

    private void IniMyStuff()
    {
      //tänne kaikki tarvittavat alustukset
      ctx = new BookShopEntities();
      ctx.Books.Load();
      localBooks = ctx.Books.Local;
      //comboboxin täyttäminen kirjojen eri mailla
      cbCountries.DataContext = localBooks.Select(n => n.country).Distinct();
      cbCountries.Visibility = Visibility.Visible;
      //view kirjojen filtterointia varten
      view = CollectionViewSource.GetDefaultView(localBooks);
    }
    private void btnHaeAsiakkaat_Click(object sender, RoutedEventArgs e)
    {
      var customers = ctx.Customers.ToList();
      dgBooks.DataContext = customers;
      IsBooks = false;
    }

    private void btnHaeKirjat_Click(object sender, RoutedEventArgs e)
    {
      dgBooks.DataContext = localBooks;
      IsBooks = true;
      cbCountries.SelectedIndex = -1;
    }

    private void dgBooks_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      //valittu item (tässä tapauksessa Customer-olio) asetetaan stackpanelin dataconteksiksi
      if (IsBooks)
        spBook.DataContext = dgBooks.SelectedItem;
      else 
        spCustomer.DataContext = dgBooks.SelectedItem;
    }

    private void btnTallenna_Click(object sender, RoutedEventArgs e)
    {
      //tallennetaan kirjaan tehdyt muutokset kantaan
      try
      {
        ctx.SaveChanges();
      }
      catch (Exception ex)
      {
        tbMessage.Text = ex.Message;
      }
    }

    private void btnUusi_Click(object sender, RoutedEventArgs e)
    {
      //luodaan uusi kirja-olio ensin kontekstiin ja sitten tietokantaan
      Book newBook;
      if (btnUusi.Content.ToString() == "Uusi")
      {
        //luodaan uusi Book-olio
        newBook = new Book();
        newBook.name = "Anna kirjan nimi";
        spBook.DataContext = newBook;
        btnUusi.Content = "Tallenna kantaan";
      }
      else
      {
        //lisätään uusi kirja ensin kontekstiiin ja sieltä kantaan
        newBook = (Book)spBook.DataContext;
        ctx.Books.Add(newBook);
        ctx.SaveChanges();
        btnUusi.Content = "Uusi";
        tbMessage.Text = "Kirja " + newBook.DisplayName + " lisätty kantaan...";
      }
    }

    private void btnPoista_Click(object sender, RoutedEventArgs e)
    {
      //poistetaan valittu kirja-olio kontekstiksi ja sitten kannasta
      Book current = (Book)spBook.DataContext;
      var retval = MessageBox.Show("Haluatko varmasti poistaa kirjan " + current.DisplayName,
        "Wanhat kirjat kysyy", MessageBoxButton.YesNo);
      if (retval == MessageBoxResult.Yes)
      {
        ctx.Books.Remove(current);
        ctx.SaveChanges();
      }
    }

    private void cbCountries_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      //suodatetaan kirjat käyttäjän valinnan mukaan
      //suodatus tehdään ns predikaatti-funktiolla
      view.Filter = MyCountryFilter;
    }

    private bool MyCountryFilter(object item)
    {
      if (cbCountries.SelectedIndex == -1)
        return true;
      else
        return (item as Book).country.Contains(cbCountries.SelectedItem.ToString());
    }
    private void btnHaeTilaukset_Click(object sender, RoutedEventArgs e)
    {
      //haetaan valitun asiakkaan tilaukset navigation proeties avulla
      string msg = "";
      Customer current = (Customer)spCustomer.DataContext;
      msg += string.Format("Asiakkaalla {0} on {1} tilausta:\n", current.DisplayName, current.OrderCount);
      foreach (var item in current.Orders)
      {
        msg += string.Format("Tilaus {0} sisältää {1} tilausriviä:\n", 
          item.odate, item.Orderitems.Count);
        //kunkin tilauksen rivit ja sitä vastaava kirja
        Decimal summa = 0;
        foreach (var oitem in item.Orderitems)
        {
          msg += string.Format("- kirja {0} kappaletta {1}\n", oitem.Book.name, oitem.count);
          summa += oitem.count * oitem.Book.price.Value;
        }
        msg += string.Format("-- tilaus yhteensä {0}\n", summa);
      }
      MessageBox.Show(msg);
    }
  }
}

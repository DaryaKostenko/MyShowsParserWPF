using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HtmlAgilityPack;
using LiteDB;

namespace MyShowsParserWPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int choise;
        public const string NameDb = "ShowsDB.db";
        public const string NameCollection = "showsID";

        public MainWindow()
        {
            InitializeComponent();
        }

        //поиск информации по ключу
        private void GetShowInfo(string id,TextBlock tb,Image img,TextBlock bad)
        {
            ShowInfo show = new ShowInfo();
            string htmlShowId = "https://myshows.me/view/" + id + "/";
            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = web.Load(htmlShowId);

            try
            {
                show.Id = id;
                //Название сериала
                show.Name = htmlDoc.DocumentNode.SelectSingleNode("//main/h1[@itemprop='name']").InnerText.Trim(); ;
                //Оригинальное название
                show.OriginalName = htmlDoc.DocumentNode.SelectSingleNode("//main/p[@class='subHeader']").InnerText.Trim();
                show.Image = htmlDoc.DocumentNode.SelectSingleNode(".//div[@class = 'presentBlock']").InnerHtml.Trim().Substring(34).Remove(79);
                //информация из таблицы
                var info = htmlDoc.DocumentNode.SelectNodes(".//div[@class = 'clear']/p");
                foreach (var str in info)
                {
                    if (str.InnerText.Contains("Страна"))
                        show.Country = str.InnerText.Trim().Substring(8);

                    else if (str.InnerText.Contains("Жанры"))
                        show.Genres = str.InnerText.Replace(" ", string.Empty).Replace("\n", " ").Substring(7);
                    
                    else if (str.InnerText.Contains("Рейтинг MyShows"))
                        show.MyShowsRating =
                            str.InnerText.Trim().Replace("\n", " ").Replace("&thinsp;", string.Empty).Substring(17);
                }
                AddShowInDB(show);
                AddShowInDb_Entity(show);
                PrintShowInfo(show, tb,img);
            }
            catch (Exception)
            {
                bad.Text = "Неверный ключ!";
            }

        }

        //возвращает ид сериала при поиске по слову 
        private string GetShowId(string htmlShowId)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = web.Load(htmlShowId);
            try
            {
                //ссылка на первый найденный сериал
                string link =
                    htmlDoc.DocumentNode.SelectSingleNode("//main/table[@class='catalogTable']/tr/td/a").Attributes[0]
                        .Value.Substring(24);// выделить ид
                return link.Remove(link.Length - 1);//удалить символ / в конце
            }
            catch (Exception)
            {
                badSearch_Word.Text = "По данному запросу ничего не найдено!";
                return String.Empty;
            }
        }

        //вывод информации о сериале
        private void PrintShowInfo(ShowInfo show, TextBlock tb,Image img)
        {
            Uri uri = new Uri(show.Image);
            ImageSource source = new BitmapImage(uri);
            img.Source = source;
            img.Stretch = Stretch.UniformToFill;
            tb.Text  = "Сериал: " + show.Name + "\n";
            tb.Text += "Оригинальное название: " + show.OriginalName + "\n";
            tb.Text += "Страна: " + show.Country + "\n";
            tb.Text += "Жанры: " + show.Genres + "\n";
            tb.Text += "Рейтинг MyShows: " + show.MyShowsRating + "\n";
        }

        //добавление в кэш
        private void AddShowInDB(ShowInfo show)
        {
            // открывает базу данных, если ее нет - то создает
            using (var db = new LiteDatabase(NameDb))
            {
                // Получаем коллекцию
                var collectionShows = db.GetCollection<ShowInfo>(NameCollection);
                //добавляем новый элемент
                collectionShows.Insert(show);
            }
        }

        //поиск в кэше
        private ShowInfo SearchInDB_ID(string ID)
        {
            // открывает базу данных, если ее нет - то создает
            using (var db = new LiteDatabase(NameDb))
            {
                // Получаем коллекцию
                var collectionShows = db.GetCollection<ShowInfo>(NameCollection);
                var resultSearch = collectionShows.FindOne(x => x.Id.Equals(ID));
                return resultSearch;
            }
        }

        //добавить в базу данных
        private void AddShowInDb_Entity(ShowInfo show)
        {
            using (var db = new Context())
            {
                var country = db.Countries.Find(show.Country);
                if (country == null)
                {
                    country = new CountryModel()
                    {
                        Name = show.Country
                    };
                }

                var new_show = new ShowModel()
                {
                    Name = show.Name,
                    OriginalName = show.OriginalName,
                    Country = country,
                    Genres = show.Genres,
                    MyShowsRating = show.MyShowsRating
                };

                db.Shows.Add(new_show);
                db.SaveChanges();
            }
        }

        //поиск всех фильмов одного автора
        private List<ShowModel> GetShowsByCountry(string country)
        {
            using (var db = new Context())
            {
                return
                   db.Shows.Where(x => x.Country.Name.ToLower() == country.ToLower()).ToList();
            }
        }

        //поиск по ID
        private void bt_Id_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var id = tb_Id.Text;
                var searchRes = SearchInDB_ID(id);
                if (searchRes == null) //если в кэше нет
                    GetShowInfo(id, TextBlockID, Img_Id,badSearch_ID);
                else
                {
                    PrintShowInfo(searchRes, TextBlockID, Img_Id);
                    TextBlockID.Text += "(Информация из кэша)";
                }
            }
            catch (Exception)
            {
                badSearch_ID.Text = "По данному запросу ничего не найдено!";
            }

        }

        //поиск по ключевому слову
        private void bt_Word_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var word = tb_Word.Text;
                string htmlShowWord = "https://myshows.me/search/?q=" + word;
                var id = GetShowId(htmlShowWord);
                if (id == String.Empty)
                {
                    badSearch_Word.Text = "По данному запросу ничего не найдено!";
                    return;
                }
                var searchRes = SearchInDB_ID(id);
                if (searchRes == null) //если в кэше нет
                    GetShowInfo(id, TextBlockWord, Img_Word,badSearch_Word);
                else
                {
                    PrintShowInfo(searchRes, TextBlockWord, Img_Word);
                    TextBlockWord.Text += "(Информация из кэша)";
                }
            }
            catch (Exception)
            {
                badSearch_Word.Text = "По данному запросу ничего не найдено!";
            }
        }

        private void bt_Country_Click(object sender, RoutedEventArgs e)
        {
            var country = tb_Country.Text;
            var list = GetShowsByCountry(country);
            if (list.Count != 0)
                DataGridInfo.ItemsSource = list;
            else
                badSearch_Country.Text = "По данному запросу ничего не найдено";
        }
        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using LiteDB;

namespace MyShowsParserWPF
{
    class Parse
    {
        public const string NameDb = "ShowsDB.db";
        public const string NameCollection = "showsID";

        //поиск информации по ключу
        public static ShowInfo GetShowInfo(string id)
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
                return show;
            }
            catch (Exception)
            {
                return null;
            }

        }

        //возвращает ид сериала при поиске по слову 
        public static string GetShowId(string htmlShowId)
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
                return String.Empty;
            }
        }
        
        //добавление в кэш
        public static void AddShowInDB(ShowInfo show)
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
        public static ShowInfo SearchInDB_ID(string ID)
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
        public static void AddShowInDb_Entity(ShowInfo show)
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
        public static List<ShowModel> GetShowsByCountry(string country)
        {
            using (var db = new Context())
            {
                return
                   db.Shows.Where(x => x.Country.Name.ToLower() == country.ToLower()).ToList();
            }
        }

        //поиск фильма по Id
        public static ShowInfo GetShowById(string id)
        {
            try
            {
                var searchRes = SearchInDB_ID(id);
                if (searchRes == null) //если в кэше нет
                    return GetShowInfo(id);
                else
                {
                    return searchRes;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        //поиск фильма по ключевому слову
        public static ShowInfo GetShowByWord(string word)
        {
            try
            {
                string htmlShowWord = "https://myshows.me/search/?q=" + word;
                var id = GetShowId(htmlShowWord);
                if (id == String.Empty)
                {
                    return null;
                }
                var searchRes = SearchInDB_ID(id);
                if (searchRes == null) //если в кэше нет
                    return GetShowInfo(id);
                else
                {
                    return searchRes;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

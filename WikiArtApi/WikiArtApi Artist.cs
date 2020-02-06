using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Globalization;
using System.Drawing;
using System.Net;
using System.IO;

namespace DabbledThings.WikiArtApi
{
    /// <summary>
    /// An object for grabbing information from WikiArt.org
    /// </summary>
    public partial class WikiArtApi
    {
        /// <summary>
        /// An artist listed on WikiArt
        /// </summary>
        public class Artist
        {
            private WikiArtApi api;
            private string name, movement, genre, school, born, died;
            private List<string> nationalities = new List<string>();
            private List<string> fields = new List<string>();
            private List<Artist> influencedBy = new List<Artist>();
            private List<Artist> influencedOn = new List<Artist>();
            private List<Artist> teachers = new List<Artist>();
            private List<Artist> pupils = new List<Artist>();
            private List<Artwork> works = new List<Artwork>();
            private Uri imageUrl;
            private bool detailsPulledFromPage = false;
            
            public List<Artwork> Works { get { if (this.works.Count == 0) { this.GetAllArtworks(); } return this.works; } }
            public string Name { get { return name; } }
            public string Movement { get { if (!this.detailsPulledFromPage && this.movement == null) { this.PullDetailsFromPage(); } return this.movement; } }
            public string Genre { get { if (!this.detailsPulledFromPage && this.genre == null) { this.PullDetailsFromPage(); } return genre; } }
            public string School { get { if (!this.detailsPulledFromPage && this.school == null) { this.PullDetailsFromPage(); } return school; } }
            public List<string> Nationalities { get { if (!this.detailsPulledFromPage && this.nationalities.Count == 0) { this.PullDetailsFromPage(); } return nationalities; } }
            public List<string> Fields { get { if (!this.detailsPulledFromPage && this.fields.Count == 0) { this.PullDetailsFromPage(); } return fields; } }
            public List<Artist> InfluencedOn { get { if (!this.detailsPulledFromPage && this.influencedOn.Count == 0) { this.PullDetailsFromPage(); } return influencedOn; } }
            public List<Artist> InfluencedBy { get { if (!this.detailsPulledFromPage && this.influencedBy.Count == 0) { this.PullDetailsFromPage(); } return influencedBy; } }
            public List<Artist> Teachers { get { if (!this.detailsPulledFromPage && this.teachers.Count == 0) { this.PullDetailsFromPage(); } return teachers; } }
            public List<Artist> Pupils { get { if (!this.detailsPulledFromPage && this.pupils.Count == 0) { this.PullDetailsFromPage(); } return pupils; } }
            public string Born { get { if (!this.detailsPulledFromPage && this.born == null) { this.PullDetailsFromPage(); } return born; } }
            public string Died { get { if (!this.detailsPulledFromPage && this.died == null) { this.PullDetailsFromPage(); } return died; } }
            public Uri PageUrl { get { return new Uri(this.api.ROOT_URL, this.PageRelativePath()); } }
            public Uri WorksListUrl { get { return new Uri(this.api.ROOT_URL, this.WorksListRelativePath()); } }
            
            internal Artist(WikiArtApi api, string name, bool validate = false)
            {
                this.api = api;
                this.name = name;

                if (validate) {
                    try { using (WebClient wc = new WebClient()) { wc.DownloadData(this.PageUrl); } }
                    catch (System.Net.WebException e) { if ((e.Response as HttpWebResponse).StatusCode == HttpStatusCode.NotFound) { throw new Exception("Artist page not found."); } }
                }
            }
            internal Artist(WikiArtApi api, Uri url, bool validate = false)
            {
                this.api = api;
                this.name = this.GetArtistNameFromUrl(url);

                if (validate) {
                    try { using (WebClient wc = new WebClient()) { wc.DownloadData(this.PageUrl); } }
                    catch (System.Net.WebException e) { if ((e.Response as HttpWebResponse).StatusCode == HttpStatusCode.NotFound) { throw new Exception("Artist page not found."); } }
                }
            }

            private void PullDetailsFromPage()
            {
                HtmlDocument doc = this.api.webHandler.Load(new Uri(this.api.ROOT_URL, this.PageUrl));
                
                this.imageUrl = new Uri(doc.DocumentNode.Descendants("img").Where(i => i.Attributes["itemprop"].Value == "image").First().Attributes["src"].Value);
                this.born = doc.DocumentNode.Descendants("span").Where(i => i.Attributes.Contains("itemprop") && i.Attributes["itemprop"].Value == "birthDate").First().InnerText;
                this.died = doc.DocumentNode.Descendants("span").Where(i => i.Attributes.Contains("itemprop") && i.Attributes["itemprop"].Value == "deathDate").First().InnerText;

                var nationalityNodes = doc.DocumentNode.Descendants("span").Where(i => i.Attributes.Contains("itemprop") && i.Attributes["itemprop"].Value == "nationality");
                nationalityNodes.ToList().ForEach(n => this.nationalities.Add(n.InnerText));

                var dictNodes = doc.DocumentNode.Descendants("li").Where(i => i.Attributes.Contains("class") && i.Attributes["class"].Value.Trim() == "dictionary-values");
                this.api.SetValueFromNode(ref this.fields, dictNodes, "Field:");
                this.api.SetValueFromNode(ref this.school, dictNodes, "Painting School:");
                this.api.SetValueFromNode(ref this.genre, dictNodes, "Genre:");
                this.api.SetValueFromNode(ref this.movement, dictNodes, "Art Movement:");
                this.api.SetValueFromNode(ref this.influencedBy, dictNodes, "Influenced by:");
                this.api.SetValueFromNode(ref this.influencedOn, dictNodes, "Influenced on:");
                this.api.SetValueFromNode(ref this.teachers, dictNodes, "Teachers:");
                this.api.SetValueFromNode(ref this.pupils, dictNodes, "Pupils:");

                this.detailsPulledFromPage = true;
            }

            private void GetAllArtworks()
            {
                HtmlDocument doc = this.api.webHandler.Load(this.WorksListUrl);
                var anchorNodes = doc.DocumentNode.Descendants("main").First().Descendants("a").Skip(5).ToList();
                foreach (HtmlNode node in anchorNodes) {
                    Uri artworkUri = new Uri(this.api.ROOT_URL, node.Attributes["href"].Value);
                    if (!artworkUri.Host.ToLower().Contains("wikiart")) { continue; }
                    this.works.Add(new Artwork(api, this, artworkUri));
                }
            }

            private bool UrlIsValid(Uri url)
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "HEAD";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                response.Close();
                return response.StatusCode == HttpStatusCode.OK;
            }
            private string PageRelativePath() { return string.Format("/en/{0}", this.Name.ToLower().Replace(' ', '-')); }
            private string WorksListRelativePath() { return string.Format("/en/{0}/all-works/text-list", TitleCasedToUrlStyle(this.Name)); }
            private string GetArtistNameFromUrl(Uri url) { return UrlStyledToTitleCase(url.ToString().Substring(url.ToString().LastIndexOf("/en/") + 4)); }
        }
    }
    
}

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
using System.Web;

namespace DabbledThings.WikiArtApi
{
    /// <summary>
    /// An object for grabbing information from WikiArt.org
    /// </summary>
    public partial class WikiArtApi
    {
        /// <summary>
        /// A piece of artwork listed on WikiArt
        /// </summary>
        public class Artwork
        {
            private WikiArtApi api;
            private string title, style, genre;
            private List<string> tags = new List<string>();
            private List<string> media = new List<string>();
            private Tuple<double?, double?> dimensionsCm = new Tuple<double?,double?>(null, null);
            private Uri imageUrl;
            private bool detailsPulledFromPage = false;

            public string Title { get { if (!this.detailsPulledFromPage && this.title == null) { this.PullDetailsFromPage(); } return this.title; } }
            public string Style { get { if (!this.detailsPulledFromPage && this.style == null) { this.PullDetailsFromPage(); } return this.style; } }
            public string Genre { get { if (!this.detailsPulledFromPage && this.genre == null) { this.PullDetailsFromPage(); } return this.genre; } }
            public List<string> Tags { get { if (!this.detailsPulledFromPage && this.tags == null) { this.PullDetailsFromPage(); } return this.tags; } }
            public List<string> Media { get { if (!this.detailsPulledFromPage && this.media == null) { this.PullDetailsFromPage(); } return this.media; } }
            public Tuple<double?, double?> DimensionsCm { get { if (!this.detailsPulledFromPage && this.dimensionsCm == null) { this.PullDetailsFromPage(); } return this.dimensionsCm; } }
            public Uri ImageUrl { get { if (!this.detailsPulledFromPage && this.imageUrl == null) { this.PullDetailsFromPage(); } return this.imageUrl; } }

            public Uri PageUrl;
            public int? Year;
            public Artist Artist;

            internal Artwork(WikiArtApi api, Artist artist, Uri url)
            {
                this.PageUrl = url;
                this.Artist = artist;
                this.title = TitleAndYearFromTitleString(WebUtility.UrlDecode(url.Segments.Last()), out this.Year);

                this.api = api;
            }

            public Stream GetImageStream()
            {
                using (WebClient wc = new WebClient()) {
                    return wc.OpenRead(this.ImageUrl);
                }
            }

            public Bitmap GetImageBitmap()
            {
                using (Stream s = this.GetImageStream()) {
                    return new Bitmap(s);
                }
            }

            private void PullDetailsFromPage()
            {
                HtmlDocument doc = this.api.webHandler.Load(new Uri(this.api.ROOT_URL, this.PageUrl));
                
                this.imageUrl = new Uri(WebUtility.HtmlDecode(doc.DocumentNode.Descendants("img").Where(i => i.Attributes["itemprop"].Value == "image").First().Attributes["src"].Value));
                
                var dictNodes = doc.DocumentNode.Descendants("li").Where(i => i.Attributes.Contains("class") && i.Attributes["class"].Value.Trim() == "dictionary-values");
                var dictTagNodes = doc.DocumentNode.Descendants("span").Where(i => i.Attributes.Contains("itemprop") && i.Attributes["itemprop"].Value.Trim() == "keywords");
                this.api.SetValueFromNode(ref this.style, dictNodes, "Style:");
                this.api.SetValueFromNode(ref this.genre, dictNodes, "Genre:");
                this.api.SetValueFromNode(ref this.tags, dictTagNodes, "");
                this.api.SetValueFromNode(ref this.media, dictNodes, "Media:");
                this.api.GetDimensionsFromArtworkPage(ref this.dimensionsCm, doc.DocumentNode.Descendants("li"), "Dimensions:");

                this.detailsPulledFromPage = true;
            }

            /// <summary>
            /// Given a title string from a WikiArt url, check if the last section of it is a year, returning the title and the year if it exists (or null otherwise).
            /// </summary>
            /// <param name="titleString">The title from the WikiArt url</param>
            /// <param name="year">An output variable for the year, in case it exists in the url</param>
            /// <returns></returns>
            private string TitleAndYearFromTitleString(string titleString, out int? year)
            {
                int tmp;

                int lastHyphen = titleString.LastIndexOf("-");
                if (lastHyphen != -1) { 
                    string afterLastHyphen = titleString.Substring(lastHyphen + 1);
                    if (int.TryParse(afterLastHyphen, out tmp)) {
                        year = tmp;
                        return UrlStyledToTitleCase(titleString.Substring(0, lastHyphen));
                    }
                }

                year = null;
                return UrlStyledToTitleCase(titleString);
            }
        }
    }
    
}

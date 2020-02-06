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
        private Uri ROOT_URL = new Uri("https://www.wikiart.org/");
        private HtmlWeb webHandler = new HtmlWeb();

        private Dictionary<string, Artist> allArtists = new Dictionary<string, Artist>();
        private List<char> pulledLetters = new List<char>();
        
        /// <summary>
        /// Given an artist's name, return an Artist object
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Artist GetArtist(string name)
        {
            if (!this.allArtists.ContainsKey(name)) {
                Artist thisArtist = new Artist(this, name, true);
                this.allArtists.Add(thisArtist.Name, thisArtist); 
            }

            return this.allArtists[name];
        }
        /// <summary>
        /// Grab an Artist object for every artist on WikiArt.org
        /// </summary>
        /// <returns></returns>
        public List<Artist> GetAllArtists()
        {
            if(this.allArtists.Count == 0) {
                for (char c = 'a'; c <= 'z'; c++) {
                    this.PullArtistsByLetter(c);
                }
            }
            return this.allArtists.Select(kv => kv.Value).ToList();
        }
        /// <summary>
        /// Grab an Artist object for every artist on WikiArt.org whose name begins with a given letter
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public List<Artist> GetArtistsByLetter(char c)
        {
            this.PullArtistsByLetter(c);
            return this.allArtists.Where(kv => kv.Key.ToLower().StartsWith(c.ToString().ToLower())).Select(kv => kv.Value).ToList();
        }

        // ==== Helper Classes for pulling data from pages for Artist and Artwork objects ====
        internal void SetValueFromNode(ref string fieldToSet, IEnumerable<HtmlNode> dictNodes, string labelOfNodeToFind)
        {
            var nodeToFindList = dictNodes.Where(n => n.InnerText.Contains(labelOfNodeToFind));
            if (nodeToFindList.Count() == 0) return;
            var nodeToFind = nodeToFindList.First();
            fieldToSet = nodeToFind.Descendants("a").ToList()[0].InnerText;
        }
        internal void SetValueFromNode(ref List<string> fieldToSet, IEnumerable<HtmlNode> dictNodes, string labelOfNodeToFind)
        {
            var nodeToFindList = dictNodes.Where(n => n.InnerText.Contains(labelOfNodeToFind));
            if (nodeToFindList.Count() == 0) return;
            var nodeToFind = nodeToFindList.First();
            foreach (HtmlNode node in nodeToFind.Descendants("a").ToList()) {
                fieldToSet.Add(node.InnerText);
            }
        }
        internal void SetValueFromNode(ref List<Artist> fieldToSet, IEnumerable<HtmlNode> dictNodes, string labelOfNodeToFind)
        {
            var nodeToFindList = dictNodes.Where(n => n.InnerText.Contains(labelOfNodeToFind));
            if (nodeToFindList.Count() == 0) return;
            var nodeToFind = nodeToFindList.First();
            foreach (HtmlNode node in nodeToFind.Descendants("a").ToList()) {
                Uri artistLink = new Uri(this.ROOT_URL, node.Attributes["href"].Value);
                if (!IsArtistPageUrl(artistLink)) continue;
                fieldToSet.Add(this.GetArtist(node.InnerText));
            }
        }
        internal void GetDimensionsFromArtworkPage(ref Tuple<double?,double?> fieldToSet, IEnumerable<HtmlNode> dictNodes, string labelOfNodeToFind)
        {
            var nodeToFindList = dictNodes.Where(n => n.InnerText.Contains(labelOfNodeToFind));
            if (nodeToFindList.Count() == 0) return;
            var nodeToFind = nodeToFindList.First();
            string dimensions = nodeToFind.Descendants("s").ToList()[0].ParentNode.InnerText.Replace("Dimensions:","").Replace("\n","").Replace(" ","").Replace("cm","").Trim();
            List<string> dimensionValues = dimensions.Split('x').ToList();
            fieldToSet = new Tuple<double?, double?>(double.Parse(dimensionValues[0]), double.Parse(dimensionValues[1]));
        }
        // ==== /Helper Classes ====

        /// <summary>
        /// Check if a given url points to an artist's page (or false, for something else like a movement)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal static bool IsArtistPageUrl(Uri url) { return !url.ToString().Contains("artists-by-art-movement");}
        private void PullArtistsByLetter(char c)
        {
            if (!this.pulledLetters.Contains(c)) {
                HtmlDocument doc = this.webHandler.Load(new Uri(ROOT_URL, this.GetArtistsByLetterRelativePath(c)));
                var anchorNodes = doc.DocumentNode.Descendants("main").First().Descendants("a").Skip(4).ToList();
                anchorNodes.ForEach(a =>
                {
                    Artist thisArtist = new Artist(this, new Uri(ROOT_URL + a.Attributes["href"].Value));
                    if (!this.allArtists.ContainsKey(thisArtist.Name)) { this.allArtists.Add(thisArtist.Name, thisArtist); }
                });
                this.pulledLetters.Add(c);
            }
        }
        private string GetArtistsByLetterRelativePath(char c) { return string.Format("/en/Alphabet/{0}/text-list", c); }
        internal static string TitleCasedToUrlStyle(string titleCased) { return titleCased.ToLower().Replace(' ', '-'); }
        internal static string UrlStyledToTitleCase(string urlStyled) { return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(urlStyled.ToString().Replace('-', ' ')); }
    }
    
}

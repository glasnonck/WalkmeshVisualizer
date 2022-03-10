﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace WalkmeshVisualizerWpf.Models
{
    public static class XmlGameData
    {
        #region Constants
        private const string XML_GAME = "Game";
        private const string XML_GAMES = "Games";
        private const string XML_KOTOR1 = "KotOR 1";
        private const string XML_KOTOR2 = "KotOR 2";
        #endregion

        public static XmlGame Kotor1Xml { get; private set; }
        public static XmlGame Kotor2Xml { get; private set; }

        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            if (IsInitialized) return;

            var path = Path.Combine(Environment.CurrentDirectory, @"Resources\GameData.xml");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"XmlGameData parsing error: Unable to find file at {path}.");
            }

            var doc = XDocument.Load(path);
            var element = doc.Descendants(XML_GAMES).FirstOrDefault();
            if (element == null)
            {
                throw new InvalidDataException($"XmlGameData parsing error: Missing '{nameof(XML_GAMES)}' element.");
            }

            var games = element.Descendants(XML_GAME);
            if (!games.Any())
            {
                throw new InvalidDataException($"XmlGameData parsing error: '{nameof(XML_GAMES)}' element is empty.");
            }

            foreach (var game in games)
            {
                var data = XmlGame.Create(game);

                switch (data.Name)
                {
                    case XML_KOTOR1:
                        Kotor1Xml = data;
                        break;
                    case XML_KOTOR2:
                        Kotor2Xml = data;
                        break;
                    default:
                        throw new InvalidDataException($"XmlGameData parsing error: Unrecognized game name '{data.Name}'.");
                }
            }

            IsInitialized = true;
        }
    }

    public class XmlGame
    {
        #region Constants
        private const string XML_NAME = "Name";
        private const string XML_RIM = "Rim";
        private const string XML_RIMS = "Rims";
        #endregion

        public string Name { get; set; }
        public List<XmlRim> Rims { get; set; }

        private XmlGame() { }

        internal static XmlGame Create(XElement gameElement)
        {
            if (gameElement == null)
            {
                throw new ArgumentNullException($"XmlGame parsing error: Argument '{nameof(gameElement)}' cannot be null.");
            }

            var nameAttr = gameElement.Attribute(XML_NAME);
            if (nameAttr == null)
            {
                throw new InvalidDataException($"XmlGame parsing error: Missing '{nameof(XML_NAME)}' attribute.");
            }

            var rimsElement = gameElement.Descendants(XML_RIMS).FirstOrDefault();
            if (rimsElement == null)
            {
                throw new InvalidDataException($"XmlGame parsing error: Missing '{nameof(XML_RIMS)}' element.");
            }

            var rims = rimsElement.Descendants(XML_RIM);
            if (!rims.Any())
            {
                throw new InvalidDataException($"XmlGame parsing error: '{nameof(XML_RIMS)}' element is empty.");
            }

            var rimList = new List<XmlRim>();
            foreach (var rim in rims)
            {
                rimList.Add(XmlRim.Create(rim));
            }

            return new XmlGame
            {
                Name = nameAttr.Value,
                Rims = rimList,
            };
        }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, # of {nameof(Rims)}: {Rims?.Count ?? -1}";
        }
    }

    public class XmlRim
    {
        #region Constants
        private const string XML_FILE_NAME = "FileName";
        private const string XML_PLANET = "Planet";
        private const string XML_COMMON_NAME = "CommonName";
        #endregion

        public string FileName { get; set; }
        public string Planet { get; set; }
        public string CommonName { get; set; }

        private XmlRim() { }

        internal static XmlRim Create(XElement rimElement)
        {
            if (rimElement == null)
            {
                throw new ArgumentNullException($"XmlRim parsing error: Argument '{nameof(rimElement)}' cannot be null.");
            }

            var fileNameElement = rimElement.Attribute(XML_FILE_NAME);
            if (fileNameElement == null)
            {
                throw new InvalidDataException($"XmlRim parsing error: Missing '{nameof(XML_FILE_NAME)}' attribute.");
            }

            var planetElement = rimElement.Attribute(XML_PLANET);
            if (planetElement == null)
            {
                throw new InvalidDataException($"XmlRim parsing error: Missing '{nameof(XML_PLANET)}' attribute.");
            }

            var commonNameElement = rimElement.Attribute(XML_COMMON_NAME);
            if (commonNameElement == null)
            {
                throw new InvalidDataException($"XmlRim parsing error: Missing '{nameof(XML_COMMON_NAME)}' attribute.");
            }

            return new XmlRim
            {
                FileName = fileNameElement.Value,
                Planet = planetElement.Value,
                CommonName = commonNameElement.Value,
            };
        }

        public override string ToString()
        {
            return $"{nameof(FileName)}: {FileName}, {nameof(Planet)}: {Planet}, {nameof(CommonName)}: {CommonName}";
        }
    }
}

using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    // SPEC-P04 §4.
    public class LocalizationTests
    {
        [TearDown]
        public void ResetLanguage()
        {
            Loc.Current = Language.En;
        }

        [Test]
        public void EveryKey_HasEnglishAndKorean_WithSamePlaceholders()
        {
            foreach (string key in Loc.Keys)
            {
                string en = Loc.Get(key, Language.En);
                string ko = Loc.Get(key, Language.Ko);
                Assert.IsFalse(string.IsNullOrWhiteSpace(en), "en missing: " + key);
                Assert.IsFalse(string.IsNullOrWhiteSpace(ko), "ko missing: " + key);
                CollectionAssert.AreEquivalent(Placeholders(key), Placeholders(en), "en placeholders: " + key);
                CollectionAssert.AreEquivalent(Placeholders(key), Placeholders(ko), "ko placeholders: " + key);
            }
        }

        [Test]
        public void DisplayedCatalogTexts_AreTranslated()
        {
            var missing = new List<string>();
            foreach (OutfitDefinition outfit in Catalog.Outfits)
            {
                Check(outfit.DisplayName, missing);
                Check(outfit.BenefitText, missing);
                Check(outfit.TradeoffText, missing);
            }
            foreach (UpgradeDefinition upgrade in Catalog.UpgradePool)
            {
                Check(upgrade.Title, missing);
                Check(upgrade.Description, missing);
            }
            UpgradeDefinition heal = Catalog.GetUpgrade(Catalog.HealFallbackId);
            Check(heal.Title, missing);
            Check(heal.Description, missing);
            Check(Catalog.GetSpirit("spirit.steam").DisplayName, missing);
            for (int floor = 1; floor <= Catalog.FloorCount; floor++)
            {
                Check(Catalog.GetFloor(floor).LearnHint, missing);
            }
            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void T_FallsBackToVietnamese_AndViIsIdentity()
        {
            Loc.Current = Language.En;
            Assert.AreEqual("Play", Loc.T("Chơi"));
            Assert.AreEqual("câu chưa dịch", Loc.T("câu chưa dịch"));
            Assert.IsNull(Loc.T(null));
            Loc.Current = Language.Vi;
            Assert.AreEqual("Chơi", Loc.T("Chơi"));
            Loc.Current = Language.Ko;
            Assert.AreEqual("플레이", Loc.T("Chơi"));
        }

        [Test]
        public void F_FormatsAndSurvivesBadArguments()
        {
            Loc.Current = Language.En;
            Assert.AreEqual("Floor 2/5", Loc.F("Tầng {0}/{1}", 2, 5));
            Assert.AreEqual("Floor {0}/{1}", Loc.F("Tầng {0}/{1}", 2));
            Loc.Current = Language.Ko;
            Assert.AreEqual("2/5층", Loc.F("Tầng {0}/{1}", 2, 5));
        }

        [Test]
        public void CodeParseNext_RoundTrip()
        {
            Assert.AreEqual(Language.En, Loc.Parse(null));
            Assert.AreEqual(Language.En, Loc.Parse("fr"));
            foreach (Language language in new[] { Language.En, Language.Vi, Language.Ko })
            {
                Assert.AreEqual(language, Loc.Parse(Loc.Code(language)));
            }
            Assert.AreEqual(Language.Vi, Loc.Next(Language.En));
            Assert.AreEqual(Language.Ko, Loc.Next(Language.Vi));
            Assert.AreEqual(Language.En, Loc.Next(Language.Ko));
        }

        [Test]
        public void SaveData_DefaultsToEnglish_AndRepairsUnknownLanguage()
        {
            Assert.AreEqual("en", SaveData.CreateDefault().language);
            SaveData data = SaveData.CreateDefault();
            data.language = "xx";
            data.Normalize();
            Assert.AreEqual("en", data.language);
            data.language = null;
            data.Normalize();
            Assert.AreEqual("en", data.language);
            data.language = "ko";
            data.Normalize();
            Assert.AreEqual("ko", data.DeepClone().language);
        }

        static void Check(string vi, List<string> missing)
        {
            if (!string.IsNullOrEmpty(vi) && !Loc.Has(vi)) missing.Add(vi);
        }

        static List<string> Placeholders(string text)
        {
            var list = new List<string>();
            foreach (Match match in Regex.Matches(text, @"\{\d+\}")) list.Add(match.Value);
            return list;
        }
    }
}

using NUnit.Framework;
using RHMG.Core;

namespace RHMG.Core.Tests
{
    public class ResultLabelLocTests
    {
        [Test]
        public void ResultScreenLabels_AreTranslatedInEveryLanguage()
        {
            Language saved = Loc.Current;
            try
            {
                foreach (string key in new[] { "Tầng đạt", "Cấp độ", "Vàng nhận", "Linh thạch", "Tinh linh", "Thời gian",
                             "Chơi lại", "Về menu", "Thất bại" })
                {
                    Assert.IsTrue(Loc.Has(key), key);
                    Loc.Current = Language.En;
                    Assert.AreNotEqual(key, Loc.T(key), "EN " + key);
                    Loc.Current = Language.Ko;
                    Assert.AreNotEqual(key, Loc.T(key), "KO " + key);
                }
                Loc.Current = Language.En;
                Assert.AreEqual("Spirits", Loc.T("Tinh linh"));
            }
            finally { Loc.Current = saved; }
        }
    }
}

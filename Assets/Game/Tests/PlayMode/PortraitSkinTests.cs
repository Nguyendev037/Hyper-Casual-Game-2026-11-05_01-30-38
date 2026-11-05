using NUnit.Framework;
using RHMG.Runtime.UI;
using UnityEngine;

namespace RHMG.PlayTests
{
    /// <summary>
    /// FIX-FLOOR item 2: Portraits/*.png are imported as plain textures, so Resources.Load&lt;Sprite&gt; is null.
    /// The HUD v2 portrait (json sprite "portrait_girl") must still resolve to a drawable sprite.
    /// </summary>
    public class PortraitSkinTests
    {
        [Test]
        public void PortraitGirl_ResolvesToSprite_EvenWhenImportedAsTexture()
        {
            Sprite sprite = UiSkin.Portrait("portrait_girl");
            Assert.IsNotNull(sprite, "UiSkin.Portrait(portrait_girl)");
            Assert.IsNotNull(sprite.texture);
            Assert.Greater(sprite.rect.width, 0f);
            Assert.AreSame(sprite, UiSkin.Portrait("portrait_girl"), "cached");
        }

        [Test]
        public void LayoutSprite_RoutesPortraitIds_ToPortraitFolder()
        {
            Assert.IsNotNull(UiLayout.Sprite("portrait_girl"), "ui_layout.json hud_portrait sprite id");
        }

        [Test]
        public void Portrait_MissingId_ReturnsNull()
        {
            Assert.IsNull(UiSkin.Portrait("portrait_does_not_exist"));
            Assert.IsNull(UiSkin.Portrait(null));
        }
    }
}

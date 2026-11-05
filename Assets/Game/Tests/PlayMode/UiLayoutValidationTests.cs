using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RHMG.Runtime.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace RHMG.PlayTests
{
    public class UiLayoutValidationTests
    {
        [TestCase("missing")]
        [TestCase("duplicate")]
        [TestCase("font")]
        [TestCase("align")]
        [TestCase("vector")]
        [TestCase("screens")]
        public void MalformedLayout_FallsBackBeforeCreatingAnyObjects(string defect)
        {
            const BindingFlags fields = BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo document = typeof(UiLayout).GetField("document", fields);
            FieldInfo loaded = typeof(UiLayout).GetField("loaded", fields);
            object original = document.GetValue(null);
            bool originalLoaded = (bool)loaded.GetValue(null);
            var host = new GameObject("LayoutValidation", typeof(RectTransform));
            try
            {
                Type reader = typeof(UiLayout).GetNestedType("Parser", BindingFlags.NonPublic);
                object parser = Activator.CreateInstance(reader, new object[] { Resources.Load<TextAsset>("RHMG/UI/ui_layout").text });
                var data = (Dictionary<string, object>)reader.GetMethod("Read").Invoke(parser, null);
                var screens = (Dictionary<string, object>)data["screens"];
                var menu = (List<object>)screens["menu"];
                var first = (Dictionary<string, object>)menu[0];
                if (defect == "missing") menu.RemoveAt(0);
                if (defect == "duplicate") menu.Add(first);
                if (defect == "font") first["font"] = "unknown-font";
                if (defect == "align") first["align"] = "not-an-anchor";
                if (defect == "vector") first["anchorMin"] = new List<object> { "zero", 1d };
                if (defect == "screens") data["screens"] = "not-an-object";
                document.SetValue(null, data);
                loaded.SetValue(null, true);
                LogAssert.Expect(LogType.Warning, new Regex(@"\[RHMG.UI\].*keeping legacy screen"));
                Assert.AreEqual(0, UiLayout.Ids("menu").Count, "Ids must validate before exposing malformed nodes");
                Assert.IsFalse(UiLayout.Apply("menu", (RectTransform)host.transform,
                    new Dictionary<string, Action<RectTransform>>()));
                Assert.AreEqual(0, host.transform.childCount);
            }
            finally
            {
                document.SetValue(null, original);
                loaded.SetValue(null, originalLoaded);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}

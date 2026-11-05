using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime.UI
{
    /// <summary>Installs the approved layout. Sample copy in the design is never game data.</summary>
    public static class UiLayout
    {
        static Dictionary<string, object> document;
        static bool loaded;
        static readonly HashSet<string> warnings = new HashSet<string>();

        public static bool Apply(string screen, RectTransform canvasRoot,
            IDictionary<string, Action<RectTransform>> binders)
        {
            if (!TryScreen(screen, out List<object> list)) return false;
            var nodes = new Dictionary<string, RectTransform>();
            foreach (object value in (List<object>)list)
            {
                var spec = Map(value);
                string id = Str(spec, "id");
                string parent = Str(spec, "parent");
                var go = new GameObject(id, typeof(RectTransform));
                RectTransform rect = (RectTransform)go.transform;
                rect.SetParent(parent != null && nodes.TryGetValue(parent, out RectTransform target)
                    ? target : canvasRoot, false);
                rect.anchorMin = Vec(spec, "anchorMin");
                rect.anchorMax = Vec(spec, "anchorMax");
                rect.pivot = Vec(spec, "pivot");
                rect.sizeDelta = Vec(spec, "size");
                rect.anchoredPosition = Vec(spec, "pos");
                string sprite = Str(spec, "sprite");
                string sample = Str(spec, "text");
                Color color = Palette(Str(spec, "color"));
                if (sprite != null || sample == null && id != "girl_preview_container")
                {
                    Image image = go.AddComponent<Image>();
                    image.sprite = sprite == null ? null : Sprite(sprite);
                    image.color = image.sprite != null ? Color.white : color;
                    image.type = image.sprite != null && image.sprite.border != Vector4.zero
                        ? Image.Type.Sliced : Image.Type.Simple;
                    image.raycastTarget = false;
                    if (id.EndsWith("_dim") || id == "dim_bg") image.color = new Color(color.r, color.g, color.b, 0.8f);
                }
                if (sample != null)
                {
                    RectTransform textRoot = rect;
                    if (sprite != null)
                    {
                        var label = new GameObject("Label", typeof(RectTransform));
                        textRoot = (RectTransform)label.transform;
                        textRoot.SetParent(rect, false);
                        textRoot.anchorMin = Vector2.zero;
                        textRoot.anchorMax = Vector2.one;
                        textRoot.offsetMin = textRoot.offsetMax = Vector2.zero;
                    }
                    Text text = textRoot.gameObject.AddComponent<Text>();
                    text.font = Str(spec, "font") == "title" ? UiSkin.Title : UiSkin.Body;
                    text.fontSize = Convert.ToInt32(spec["fontSize"]);
                    text.resizeTextForBestFit = true;
                    text.resizeTextMinSize = 24;
                    text.resizeTextMaxSize = text.fontSize;
                    text.color = color;
                    text.alignment = (TextAnchor)Enum.Parse(typeof(TextAnchor), Str(spec, "align"));
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                    text.raycastTarget = false;
                    text.text = string.Empty;
                }
                nodes.Add(id, rect);
                if (binders != null && binders.TryGetValue(id, out Action<RectTransform> bind)) bind(rect);
            }
            return true;
        }

        public static Color Palette(string token)
        {
            if (Load() && Map(document["palette"]).TryGetValue(token ?? "", out object value)
                && ColorUtility.TryParseHtmlString((string)value, out Color color)) return color;
            return Color.white;
        }

        public static Sprite Sprite(string id)
        {
            if (id == "icon_spirit_leaf") id = "icon_spirit_bud";
            if (id == "icon_spirit_bloom") id = "icon_spirit_growth";
            if (id.StartsWith("tile_") || id.StartsWith("gem_"))
                return Resources.Load<Sprite>("RHMG/Sprites/" + id);
            return id.StartsWith("icon_") ? UiSkin.Icon(id) : UiSkin.Ui(id);
        }

        public static IList<string> Ids(string screen)
        {
            var result = new List<string>();
            if (!TryScreen(screen, out List<object> list)) return result;
            foreach (object node in list) result.Add(Str(Map(node), "id"));
            return result;
        }

        static bool TryScreen(string screen, out List<object> list)
        {
            list = null;
            if (!Load()) return false;
            try
            {
                list = (List<object>)Map(document["screens"])[screen];
                return Validate(screen, list);
            }
            catch (Exception error)
            {
                Warn(error.Message + "; keeping legacy screen");
                return false;
            }
        }

        static readonly Dictionary<string, string> required = new Dictionary<string, string>
        {
            { "hud", "xp_bar,xp_bar_fill,level_badge,level_text,xp_level_label,floor_badge,timer_badge,gold_badge,gold_icon,hp_bar_frame,hp_bar_fill,hp_text,boss_bar_frame,boss_bar_fill,boss_title,btn_pause,pause_icon,spirit_dock_1,spirit_dock_2,spirit_dock_3" },
            { "levelup", "dim_bg,modal_title,modal_subtitle,card_1,card_1_icon,card_1_name,card_1_rank,card_1_desc,card_2,card_2_icon,card_2_name,card_2_rank,card_2_desc,card_3,card_3_icon,card_3_name,card_3_rank,card_3_desc,btn_confirm_card" },
            { "station", "station_bg,panel_station_modal,st_title,st_subtitle,st_slot_1,st_icon_1,st_name_1,st_desc_1,st_slot_2,st_icon_2,st_name_2,st_desc_2,st_slot_3,st_icon_3,st_name_3,st_desc_3,btn_leave_station" },
            { "wardrobe", "wardrobe_bg,btn_wardrobe_back,wardrobe_title,tab_hair,tab_outfit,tab_staff,preview_dais,girl_preview_container,item_1_card,item_1_name,badge_equipped_tag,item_2_card,item_2_name,item_3_card,item_3_name,btn_equip_action" },
            { "menu", "menu_bg,game_logo_text,game_subtitle_text,btn_menu_start,btn_menu_continue,btn_menu_wardrobe,btn_menu_spirits,btn_menu_settings,menu_version" },
            { "pause", "pause_dim,pause_panel,pause_title,pause_quote,pause_stats_card,recap_floor_txt,recap_spirits_txt,recap_cards_txt,recap_gold_txt,recap_time_txt,btn_pause_resume,btn_pause_restart,btn_pause_exit" },
            { "result", "result_dim,result_panel,result_badge_top,result_title,result_subtitle,result_stat_card,stat_floor_lbl,stat_floor_val,stat_level_lbl,stat_level_val,stat_gold_lbl,stat_gold_val,stat_kills_lbl,stat_kills_val,stat_spirits_lbl,stat_spirits_val,stat_time_lbl,stat_time_val,btn_result_again,btn_result_menu" },
        };

        static bool Validate(string screen, List<object> list)
        {
            try
            {
                var ids = new HashSet<string>();
                var palette = Map(document["palette"]);
                var fonts = Map(document["fonts"]);
                foreach (object value in list)
                {
                    var node = Map(value);
                    foreach (string key in new[] { "id", "parent", "sprite", "text", "font", "color", "align", "fontSize" })
                        if (!node.ContainsKey(key)) throw new FormatException("Missing " + key);
                    string id = node["id"] as string;
                    if (string.IsNullOrEmpty(id) || !ids.Add(id)) throw new FormatException("Invalid or duplicate id " + id);
                    foreach (string key in new[] { "parent", "sprite", "text" })
                        if (node[key] != null && !(node[key] is string)) throw new FormatException(id + ": invalid " + key);
                    string parent = Str(node, "parent");
                    if (parent != null && (parent == id || !ids.Contains(parent)))
                        throw new FormatException("Missing parent " + parent);
                    foreach (string key in new[] { "anchorMin", "anchorMax", "pivot", "pos", "size" })
                    {
                        var pair = node[key] as List<object>;
                        if (pair == null || pair.Count != 2) throw new FormatException(id + ": invalid " + key);
                        foreach (object number in pair)
                            if (!(number is double n) || double.IsNaN(n) || double.IsInfinity(n))
                                throw new FormatException(id + ": invalid vector number");
                    }
                    string font = node["font"] as string;
                    if (font == null || !fonts.ContainsKey(font) || !(fonts[font] is string))
                        throw new FormatException(id + ": invalid font");
                    if (!(node["fontSize"] is double fontSize) || fontSize < 0 || fontSize > 512 ||
                        Str(node, "text") != null && fontSize < 1) throw new FormatException(id + ": invalid font size");
                    string color = node["color"] as string;
                    if (color == null || !palette.TryGetValue(color, out object token) ||
                        !(token is string hex) || !ColorUtility.TryParseHtmlString(hex, out _))
                        throw new FormatException(id + ": invalid color");
                    if (!(node["align"] is string alignment) || !Enum.TryParse(alignment, out TextAnchor anchor) ||
                        !Enum.IsDefined(typeof(TextAnchor), anchor)) throw new FormatException(id + ": invalid alignment");
                }
                foreach (string id in required[screen].Split(','))
                    if (!ids.Contains(id)) throw new FormatException("THIẾU: " + screen + "/" + id);
                return true;
            }
            catch (Exception error)
            {
                Warn(error.Message + "; keeping legacy screen");
                return false;
            }
        }

        static bool Load()
        {
            if (loaded) return document != null;
            loaded = true;
            TextAsset asset = Resources.Load<TextAsset>("RHMG/UI/ui_layout");
            try
            {
                if (asset != null) document = Map(new Parser(asset.text).Read());
            }
            catch (Exception error) { Warn(error.Message); }
            if (document == null) Warn("Missing or invalid RHMG/UI/ui_layout; keeping legacy layout");
            return document != null;
        }

        static void Warn(string text)
        {
            if (warnings.Add(text)) Debug.LogWarning("[RHMG.UI] " + text);
        }

        static Dictionary<string, object> Map(object value) => (Dictionary<string, object>)value;
        static string Str(Dictionary<string, object> value, string key) => value[key] as string;
        static Vector2 Vec(Dictionary<string, object> value, string key)
        {
            var list = (List<object>)value[key];
            return new Vector2(Convert.ToSingle(list[0]), Convert.ToSingle(list[1]));
        }

        // Small JSON reader: layouts contain dictionaries, arrays, strings, numbers, booleans and null.
        sealed class Parser
        {
            readonly string source;
            int at;
            public Parser(string text) { source = text; }
            void Space() { while (at < source.Length && char.IsWhiteSpace(source[at])) at++; }
            public object Read()
            {
                Space();
                char c = source[at++];
                if (c == '{')
                {
                    var map = new Dictionary<string, object>();
                    Space();
                    while (source[at] != '}')
                    {
                        string key = (string)Read();
                        Space();
                        if (source[at++] != ':') throw new FormatException("Expected colon");
                        map.Add(key, Read());
                        Space();
                        if (source[at] != ',') break;
                        at++;
                    }
                    if (source[at++] != '}') throw new FormatException("Expected object end");
                    return map;
                }
                if (c == '[')
                {
                    var list = new List<object>();
                    Space();
                    while (source[at] != ']')
                    {
                        list.Add(Read());
                        Space();
                        if (source[at] != ',') break;
                        at++;
                    }
                    if (source[at++] != ']') throw new FormatException("Expected array end");
                    return list;
                }
                if (c == '"')
                {
                    var result = new System.Text.StringBuilder();
                    while (source[at] != '"')
                    {
                        char next = source[at++];
                        if (next == '\\')
                        {
                            next = source[at++];
                            if (next == 'u')
                            {
                                next = (char)int.Parse(source.Substring(at, 4), NumberStyles.HexNumber);
                                at += 4;
                            }
                            else if (next == 'n') next = '\n';
                            else if (next == 'r') next = '\r';
                            else if (next == 't') next = '\t';
                        }
                        result.Append(next);
                    }
                    at++;
                    return result.ToString();
                }
                int start = at - 1;
                while (at < source.Length && ",]} \r\n\t".IndexOf(source[at]) < 0) at++;
                string atom = source.Substring(start, at - start);
                if (atom == "null") return null;
                if (atom == "true") return true;
                if (atom == "false") return false;
                return double.Parse(atom, CultureInfo.InvariantCulture);
            }
        }
    }
}

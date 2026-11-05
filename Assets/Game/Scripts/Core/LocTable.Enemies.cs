namespace RHMG.Core
{
    // Plan 05 WE: display names of the chapter enemies and guardians (EnemyDefinition.DisplayName is the key).
    // Kept in its own file so LocTable.cs stays owned by the UI package; merge these lines there if preferred.
    public static partial class Loc
    {
        static Loc()
        {
            Table["Quỷ Than"] = new[] { "Ember Imp", "불씨 임프" };
            Table["Tháp Tia Lửa"] = new[] { "Spark Turret", "불꽃 포탑" };
            Table["Người Đe"] = new[] { "Anvil Golem", "모루 골렘" };
            Table["Dơi Sách"] = new[] { "Book Bat", "책 박쥐" };
            Table["Bóng Gương"] = new[] { "Mirror Ghost", "거울 유령" };
            Table["Slime Mực"] = new[] { "Ink Slime", "잉크 슬라임" };
            Table["Hiệp Sĩ Dịch"] = new[] { "Plague Knight", "역병 기사" };
            Table["Xạ Thủ Xương"] = new[] { "Bone Archer", "뼈 궁수" };
            Table["Mạt Bom"] = new[] { "Bomb Mite", "폭탄 진드기" };
            Table["Sói Bóng"] = new[] { "Shadow Wolf", "그림자 늑대" };
            Table["Tinh Quang"] = new[] { "Star Wisp", "별빛 도깨비불" };
            Table["Mắt Hư Không"] = new[] { "Void Eye", "공허의 눈" };
            Table["Golem Lò Rèn"] = new[] { "Forge Golem", "용광로 골렘" };
            Table["Phù Thủy Gương"] = new[] { "Mirror Witch", "거울 마녀" };
            Table["Hiệp Sĩ Sa Ngã"] = new[] { "Corrupt Knight", "타락한 기사" };
            Table["Rồng Hoàng Hôn"] = new[] { "Twilight Drake", "황혼의 비룡" };
        }
    }
}

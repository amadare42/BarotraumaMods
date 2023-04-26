if SERVER then return end

Overrides = {
    ["entityname.amblygonite"]="Amblygonite (LiAlSo)",
    ["entityname.esperite"]="Esperite (Zn2Pb)",
    ["entityname.quartz"]="Quartz (Si2)",
    ["entityname.aragonite"]="Aragonite (Ca2)",
    ["entityname.galena"]="Galena (Pb2)",
    ["entityname.sphalerite"]="Sphalerite (Zn3)",
    ["entityname.bornite"]="Bornite (Cu2)",
    ["entityname.graphite"]="Graphite (C2)",
    ["entityname.stannite"]="Stannite (CuFeSn)",
    ["entityname.brockite"]="Brockite (ThP)",
    ["entityname.hydroxyapatite"]="Hydroxyapatite (CaP2)",
    ["entityname.sylvite"]="Sylvite (NaK)",
    ["entityname.cassiterite"]="Cassiterite (Sn3)",
    ["entityname.ilmenite"]="Ilmenite (Ti)",
    ["entityname.thorianite"]="Thorianite (Th2)",
    ["entityname.chalcopyrite"]="Chalcopyrite (Cu2)",
    ["entityname.ironore"]="Iron Ore (Fe4)",
    ["entityname.titanite"]="Titanite (Ti3Fe)",
    ["entityname.chamosite"]="Chamosite (Fe2Al2)",
    ["entityname.langbeinite"]="Langbeinite (Mg2K)",
    ["entityname.triphylite"]="Triphylite (Li)",
    ["entityname.chrysoprase"]="Chrysoprase (Si2)",
    ["entityname.lazulite"]="Lazulite (FeP)",
    ["entityname.uraniumore"]="Uranium Ore (U3)",
    ["entityname.cryolite"]="Cryolite (Na2)",
    ["entityname.polyhalite"]="Polyhalite (CaK)",
    ["entityname.diamond"]="Diamond (C3)",
    ["entityname.pyromorphite"]="Pyromorphite (Cl2)"
}
LuaUserData.MakeFieldAccessible(Descriptors["Barotrauma.TagLString"], "tags")
LuaUserData.MakeFieldAccessible(Descriptors["Barotrauma.TagLString"], "cachedValue")
LuaUserData.MakeFieldAccessible(Descriptors["Barotrauma.ItemPrefab"], "name")

function PatchItemPrefabs()
    print("patching mineral prefabs...")

    for el in ItemPrefab.Prefabs do
        if (LuaUserData.TypeOf(el.name) == "Barotrauma.TagLString") then
            local key = string.lower(tostring(el.name.tags[1]))
            local replacement = Overrides[key]
            if replacement ~= nil then
                el.name.cachedValue = replacement
            end
        end
    end

    print("mineral prefabs patched!")
end

Hook.Add("roundStart", "patch_mineral_items_prefabs", function(message, client)
    PatchItemPrefabs()
end)
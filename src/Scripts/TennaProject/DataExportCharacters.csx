#r "System.Windows.Forms"

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

string TennaSelectExportDir()
{
  string sharedExportDir = Environment.GetEnvironmentVariable("TENNA_UMT_EXPORT_DIR");
  if (!string.IsNullOrWhiteSpace(sharedExportDir))
    return sharedExportDir;

  try
  {
    using (FolderBrowserDialog dialog = new FolderBrowserDialog())
    {
      dialog.Description = "Choose export folder";
      dialog.ShowNewFolderButton = true;

      if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        return dialog.SelectedPath;
    }
  }
  catch
  {
  }

  ScriptError("Export cancelled. No folder selected.");
  throw new Exception("Export cancelled. No folder selected.");
}

void TennaExportCharacters()
{
  if (!TennaExportCharacters(TennaSelectExportDir()))
    throw new Exception("DataExportCharacters.csx failed.");
}

bool TennaExportCharacters(string exportDir)
{
  EnsureDataLoaded();
  Directory.CreateDirectory(exportDir);

  UndertaleCode gamestartSource = TennaGetRequiredScriptCode("scr_gamestart");
  UndertaleCode weaponSource = TennaGetRequiredScriptCode("scr_weaponinfo");
  UndertaleCode armorSource = TennaGetRequiredScriptCode("scr_armorinfo");

  if (gamestartSource == null || weaponSource == null || armorSource == null)
  {
    ScriptMessage("Could not find required character/weapon/armor source scripts.");
    return false;
  }

  Dictionary<int, TennaCharacter> characters = new Dictionary<int, TennaCharacter>();
  for (int id = 1; id <= 4; id++)
  {
    characters[id] = new TennaCharacter
    {
      Id = id,
      Name = TennaGetCharacterName(id)
    };
  }

  int activeChapter = TennaParseActiveChapter(GetDecompiledText(gamestartSource));
  TennaParseGamestart(GetDecompiledText(gamestartSource), activeChapter, characters);
  TennaParseWeaponConstraints(GetDecompiledText(weaponSource), characters);
  TennaParseArmorConstraints(GetDecompiledText(armorSource), characters);

  TennaWriteCharacterJson(characters, activeChapter, exportDir);
  return true;
}

UndertaleCode TennaGetRequiredScriptCode(string sourceName)
{
  UndertaleScript script = Data.Scripts.ByName(sourceName);
  if (script == null || script.Code == null)
    return null;
  return script.Code;
}

string TennaGetCharacterName(int id)
{
  if (id == 1) return "Kris";
  if (id == 2) return "Susie";
  if (id == 3) return "Ralsei";
  if (id == 4) return "Noelle";
  return "Unknown " + id;
}

int TennaParseActiveChapter(string text)
{
  using (StringReader reader = new StringReader(text))
  {
    string line;
    while ((line = reader.ReadLine()) != null)
    {
      string trimmed = line.Trim();
      if (trimmed.StartsWith("global.chapter ="))
      {
        string right = trimmed.Substring(16).Trim().TrimEnd(';');
        int ch;
        if (int.TryParse(right, out ch))
          return ch;
      }
    }
  }
  return 4; // default to 4 if not found
}

void TennaParseGamestart(string text, int activeChapter, Dictionary<int, TennaCharacter> characters)
{
  int currentChapter = -1;
  using (StringReader reader = new StringReader(text))
  {
    string line;
    while ((line = reader.ReadLine()) != null)
    {
      string trimmed = line.Trim();
      
      if (trimmed.StartsWith("if (global.chapter == "))
      {
        string numStr = trimmed.Substring(21).Replace(")", "").Replace("{", "").Trim();
        int ch;
        if (int.TryParse(numStr, out ch))
          currentChapter = ch;
        continue;
      }

      int equals = trimmed.IndexOf('=');
      if (equals > 0)
      {
        string left = trimmed.Substring(0, equals).Trim();
        string right = trimmed.Substring(equals + 1).Trim().TrimEnd(';');
        int val;
        if (int.TryParse(right, out val))
        {
          int charId = TennaParseArrayIndex(left);
          if (charId >= 1 && charId <= 4)
          {
            TennaCharacter character = characters[charId];
            if (currentChapter == activeChapter)
            {
              if (left.StartsWith("global.maxhp["))
                character.MaxHp = val;
              else if (left.StartsWith("global.hp["))
                character.Hp = val;
              else if (left.StartsWith("global.at["))
                character.Attack = val;
              else if (left.StartsWith("global.mag["))
                character.Magic = val;
              else if (left.StartsWith("global.df["))
                character.Defense = val;
              else if (left.StartsWith("global.charweapon["))
                character.Weapon = val;
              else if (left.StartsWith("global.chararmor1["))
                character.Armor1 = val;
              else if (left.StartsWith("global.chararmor2["))
                character.Armor2 = val;
            }
          }
        }
      }

      if (trimmed.StartsWith("global.spell["))
      {
        int equalsIdx = trimmed.IndexOf('=');
        if (equalsIdx > 0)
        {
          string left = trimmed.Substring(0, equalsIdx).Trim();
          string right = trimmed.Substring(equalsIdx + 1).Trim().TrimEnd(';');
          int spellId;
          if (int.TryParse(right, out spellId))
          {
            int charId = TennaParseSpellCharIndex(left);
            if (charId >= 1 && charId <= 4)
            {
              if (!characters[charId].Spells.Contains(spellId) && spellId != 0)
                characters[charId].Spells.Add(spellId);
            }
          }
        }
      }
    }
  }
}

int TennaParseArrayIndex(string text)
{
  int open = text.IndexOf('[');
  int close = text.IndexOf(']');
  if (open >= 0 && close > open)
  {
    string numStr = text.Substring(open + 1, close - open - 1);
    int val;
    if (int.TryParse(numStr, out val))
      return val;
  }
  return -1;
}

int TennaParseSpellCharIndex(string text)
{
  int open = text.IndexOf('[');
  int close = text.IndexOf(']');
  if (open >= 0 && close > open)
  {
    string numStr = text.Substring(open + 1, close - open - 1);
    int val;
    if (int.TryParse(numStr, out val))
      return val;
  }
  return -1;
}

void TennaParseWeaponConstraints(string text, Dictionary<int, TennaCharacter> characters)
{
  if (string.IsNullOrEmpty(text))
    return;

  int currentWeaponId = -1;
  int char1 = 0, char2 = 0, char3 = 0, char4 = 0;

  Action commitWeapon = () => {
    if (currentWeaponId != -1)
    {
      if (char1 == 1) characters[1].AllowedWeapons.Add(currentWeaponId);
      if (char2 == 1) characters[2].AllowedWeapons.Add(currentWeaponId);
      if (char3 == 1) characters[3].AllowedWeapons.Add(currentWeaponId);
      if (char4 == 1) characters[4].AllowedWeapons.Add(currentWeaponId);
    }
  };

  using (StringReader reader = new StringReader(text))
  {
    string line;
    while ((line = reader.ReadLine()) != null)
    {
      string trimmed = line.Trim();
      if (trimmed.StartsWith("case "))
      {
        commitWeapon();
        char1 = 0; char2 = 0; char3 = 0; char4 = 0;

        int colon = trimmed.IndexOf(':');
        if (colon > 0)
        {
          string numStr = trimmed.Substring(5, colon - 5).Trim();
          int id;
          if (int.TryParse(numStr, out id))
            currentWeaponId = id;
          else
            currentWeaponId = -1;
        }
        continue;
      }

      if (currentWeaponId == -1)
        continue;

      if (trimmed.StartsWith("break;"))
      {
        commitWeapon();
        currentWeaponId = -1;
        continue;
      }

      int equals = trimmed.IndexOf('=');
      if (equals > 0)
      {
        string left = trimmed.Substring(0, equals).Trim();
        string right = trimmed.Substring(equals + 1).Trim().TrimEnd(';');
        int val;
        if (int.TryParse(right, out val))
        {
          if (left == "weaponchar1temp") char1 = val;
          else if (left == "weaponchar2temp") char2 = val;
          else if (left == "weaponchar3temp") char3 = val;
          else if (left == "weaponchar4temp") char4 = val;
        }
      }
    }
  }
}

void TennaParseArmorConstraints(string text, Dictionary<int, TennaCharacter> characters)
{
  if (string.IsNullOrEmpty(text))
    return;

  int currentArmorId = -1;
  int char1 = 0, char2 = 0, char3 = 0, char4 = 1; // note that Noelle defaults to 1

  Action commitArmor = () => {
    if (currentArmorId != -1)
    {
      if (char1 == 1) characters[1].AllowedArmors.Add(currentArmorId);
      if (char2 == 1) characters[2].AllowedArmors.Add(currentArmorId);
      if (char3 == 1) characters[3].AllowedArmors.Add(currentArmorId);
      if (char4 == 1) characters[4].AllowedArmors.Add(currentArmorId);
    }
  };

  using (StringReader reader = new StringReader(text))
  {
    string line;
    while ((line = reader.ReadLine()) != null)
    {
      string trimmed = line.Trim();
      if (trimmed.StartsWith("case "))
      {
        commitArmor();
        char1 = 0; char2 = 0; char3 = 0; char4 = 1;

        int colon = trimmed.IndexOf(':');
        if (colon > 0)
        {
          string numStr = trimmed.Substring(5, colon - 5).Trim();
          int id;
          if (int.TryParse(numStr, out id))
            currentArmorId = id;
          else
            currentArmorId = -1;
        }
        continue;
      }

      if (currentArmorId == -1)
        continue;

      if (trimmed.StartsWith("break;"))
      {
        commitArmor();
        currentArmorId = -1;
        continue;
      }

      int equals = trimmed.IndexOf('=');
      if (equals > 0)
      {
        string left = trimmed.Substring(0, equals).Trim();
        string right = trimmed.Substring(equals + 1).Trim().TrimEnd(';');
        int val;
        if (int.TryParse(right, out val))
        {
          if (left == "armorchar1temp") char1 = val;
          else if (left == "armorchar2temp") char2 = val;
          else if (left == "armorchar3temp") char3 = val;
          else if (left == "armorchar4temp") char4 = val;
        }
      }
    }
  }
}

void TennaWriteCharacterJson(Dictionary<int, TennaCharacter> characters, int activeChapter, string exportDir)
{
  string outputPath = Path.Combine(exportDir, "characters.json");

  using (StreamWriter writer = new StreamWriter(outputPath, false))
  {
    writer.WriteLine("{");

    int charIdx = 0;
    foreach (var kvp in characters)
    {
      TennaCharacter character = kvp.Value;
      if (charIdx > 0)
        writer.WriteLine(",");

      writer.WriteLine("  \"" + character.Name.ToUpper() + "\": {");
      writer.WriteLine("    \"id\": " + character.Id + ",");
      writer.WriteLine("    \"name\": \"" + character.Name + "\",");
      writer.WriteLine("    \"chapter\": " + activeChapter + ",");
      
      // Spells
      writer.Write("    \"spells\": [");
      for (int i = 0; i < character.Spells.Count; i++)
      {
        if (i > 0) writer.Write(", ");
        writer.Write(character.Spells[i]);
      }
      writer.WriteLine("],");

      // Allowed weapons
      writer.Write("    \"allowedWeapons\": [");
      for (int i = 0; i < character.AllowedWeapons.Count; i++)
      {
        if (i > 0) writer.Write(", ");
        writer.Write(character.AllowedWeapons[i]);
      }
      writer.WriteLine("],");

      // Allowed armors
      writer.Write("    \"allowedArmors\": [");
      for (int i = 0; i < character.AllowedArmors.Count; i++)
      {
        if (i > 0) writer.Write(", ");
        writer.Write(character.AllowedArmors[i]);
      }
      writer.WriteLine("],");

      // Flat Stats
      writer.WriteLine("    \"hp\": " + character.Hp + ",");
      writer.WriteLine("    \"maxHp\": " + character.MaxHp + ",");
      writer.WriteLine("    \"attack\": " + character.Attack + ",");
      writer.WriteLine("    \"magic\": " + character.Magic + ",");
      writer.WriteLine("    \"defense\": " + character.Defense + ",");
      writer.WriteLine("    \"weapon\": " + character.Weapon + ",");
      writer.WriteLine("    \"armor1\": " + character.Armor1 + ",");
      writer.WriteLine("    \"armor2\": " + character.Armor2);

      writer.Write("  }");
      charIdx++;
    }

    writer.WriteLine();
    writer.WriteLine("}");
  }

  ScriptMessage("Exported characters data to:\n" + outputPath);
}

class TennaCharacter
{
  public int Id;
  public string Name;
  public List<int> Spells = new List<int>();
  public List<int> AllowedWeapons = new List<int>();
  public List<int> AllowedArmors = new List<int>();
  
  public int Hp = 200;
  public int MaxHp = 250;
  public int Attack = 10;
  public int Magic = 0;
  public int Defense = 2; 
  public int Weapon = 1;  
  public int Armor1 = 0;
  public int Armor2 = 0;
}

TennaExportCharacters();

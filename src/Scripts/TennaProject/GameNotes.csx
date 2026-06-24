EnsureDataLoaded();

if (Data.Code.ByName("gml_Object_obj_time_Create_0") is not UndertaleCode createCode)
{
  ScriptError("Failed to find obj_time Create event.");
  return;
}
if (Data.Code.ByName("gml_Object_obj_time_Step_1") is not UndertaleCode stepCode)
{
  ScriptError("Failed to find obj_time Step_1 event.");
  return;
}
if (Data.Code.ByName("gml_Object_obj_time_Draw_64") is not UndertaleCode drawCode)
{
  ScriptError("Failed to find obj_time Draw_64 event.");
  return;
}

string checkCreate = GetDecompiledText(createCode);

if (!checkCreate.Contains("_tenna_core_enabled"))
{
  ScriptError("Tenna Core is required!\n\nPlease install GameCore.csx first.");
  return;
}

bool notesAlreadyInstalled = checkCreate.Contains("_tenna_notes_enabled");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
  ThrowOnNoOpFindReplace = true
};

string createInit = @"
// TENNA_NOTES_CREATE_BEGIN
global._tenna_notes_enabled = true;
global._tenna_notes_open = false;
global._tenna_notes_text = """";
global._tenna_notes_msg = """";
global._tenna_notes_msg_timer = 0;
// TENNA_NOTES_CREATE_END
";

string stepCheck = @"
// TENNA_NOTES_STEP_BEGIN
if (keyboard_check_pressed(ord(""N"")) && keyboard_check(vk_alt))
{
    global._tenna_notes_open = !global._tenna_notes_open;
    keyboard_lastchar = """";
    if (global._tenna_notes_open)
    {
        global._tenna_notes_text = """";
        instance_deactivate_all(true);
        instance_activate_object(obj_time);
        instance_activate_object(obj_gamecontroller);
    }
    else
    {
        instance_activate_all();
    }
}

if (global._tenna_notes_open)
{
    if (keyboard_check_pressed(vk_escape))
    {
        instance_activate_all();
        global._tenna_notes_open = false;
        keyboard_lastchar = """";
    }
    else if (keyboard_check_pressed(vk_backspace))
    {
        if (string_length(global._tenna_notes_text) > 0)
            global._tenna_notes_text = string_copy(global._tenna_notes_text, 1, string_length(global._tenna_notes_text) - 1);
        keyboard_lastchar = """";
    }
    else if (keyboard_check_pressed(vk_enter))
    {
        if (string_length(global._tenna_notes_text) > 0)
        {
            scr_tenna_log(""Note"", global._tenna_notes_text);
            global._tenna_notes_msg = ""Logged note: "" + global._tenna_notes_text;
            global._tenna_notes_msg_timer = 150;
            global._tenna_notes_text = """";
            instance_activate_all();
            global._tenna_notes_open = false;
        }
        keyboard_lastchar = """";
    }
    else
    {
        var _k = keyboard_lastchar;
        if (_k != """")
        {
            var _c = ord(_k);
            if (_c >= 32 && _c <= 126 && string_length(global._tenna_notes_text) < 120)
                global._tenna_notes_text += _k;
            keyboard_lastchar = """";
        }
    }
}

if (global._tenna_notes_msg_timer > 0)
    global._tenna_notes_msg_timer -= 1;
// TENNA_NOTES_STEP_END
";

string drawDisplay = @"
// TENNA_NOTES_DRAW_BEGIN
if (global._tenna_notes_open)
{
    draw_set_alpha(0.85);
    draw_set_color(c_black);
    draw_rectangle(80, 155, 560, 300, false);
    draw_set_alpha(1);
    
    draw_set_font(fnt_main);
    draw_set_halign(fa_center);
    draw_set_color(c_white);
    draw_text(320, 172, ""TENNA NOTE"");
    
    draw_set_halign(fa_left);
    draw_set_color(c_yellow);
    draw_text(110, 215, global._tenna_notes_text + ""_"");
    
    draw_set_color(c_gray);
    draw_text(110, 260, ""Enter logs note  |  Esc cancels  |  Backspace deletes"");
    draw_set_halign(fa_left);
    draw_set_color(c_white);
}

if (global._tenna_notes_msg_timer > 0)
{
    draw_set_font(fnt_main);
    draw_set_halign(fa_center);
    draw_set_alpha(min(1, global._tenna_notes_msg_timer / 30));
    draw_set_color(c_black);
    draw_text(321, 401, global._tenna_notes_msg);
    draw_set_color(c_lime);
    draw_text(320, 400, global._tenna_notes_msg);
    draw_set_alpha(1);
    draw_set_halign(fa_left);
    draw_set_color(c_white);
}
// TENNA_NOTES_DRAW_END
";

try
{
  string currentStepText = GetDecompiledText(stepCode);
  string currentDrawText = GetDecompiledText(drawCode);
  string currentCreateText = GetDecompiledText(createCode);

  string cleanCreate = TennaCleanAllBlocks(currentCreateText, "global._tenna_notes_enabled = true;", "global._tenna_notes_msg_timer = 0;");
  importGroup.QueueReplace(createCode, createInit + cleanCreate);

  string cleanStep = TennaCleanAllBlocks(currentStepText, "keyboard_check_pressed(ord(\"N\"))", "global._tenna_notes_msg_timer -= 1;");
  importGroup.QueueReplace(stepCode, stepCheck + cleanStep);

  string cleanDraw = TennaCleanAllBraceBlocks(currentDrawText, "global._tenna_notes_open");
  importGroup.QueueReplace(drawCode, cleanDraw + drawDisplay);
  
  importGroup.Import();
  if (Environment.GetEnvironmentVariable("TENNA_UMT_SUPPRESS_SCRIPT_MESSAGES") != "1")
    ScriptMessage("Notes " + (notesAlreadyInstalled ? "updated" : "installed") + "!\n\nAlt+N opens the note prompt.");
}
catch (Exception ex)
{
  ScriptError($"Failed to install: {ex.Message}");
}

string TennaCleanBlock(string source, string startPattern, string endPattern)
{
  int startIdx = source.IndexOf(startPattern, StringComparison.Ordinal);
  if (startIdx < 0)
    return source;

  int ifIdx = source.LastIndexOf("if", startIdx, StringComparison.Ordinal);
  if (ifIdx >= 0 && startIdx - ifIdx < 15)
    startIdx = ifIdx;

  int endIdx = source.IndexOf(endPattern, startIdx, StringComparison.Ordinal);
  if (endIdx < 0)
    return source;

  endIdx += endPattern.Length;

  int braceCount = 0;
  while (endIdx < source.Length)
  {
    char c = source[endIdx];
    if (c == '\r' || c == '\n' || c == ' ')
    {
      endIdx++;
    }
    else if (c == '}' && braceCount < 3)
    {
      endIdx++;
      braceCount++;
    }
    else
    {
      break;
    }
  }

  return source.Substring(0, startIdx) + source.Substring(endIdx);
}

string TennaCleanAllBlocks(string source, string startPattern, string endPattern)
{
  string current = source;
  while (true)
  {
    string cleaned = TennaCleanBlock(current, startPattern, endPattern);
    if (cleaned == current)
      break;
    current = cleaned;
  }
  return current;
}

string TennaCleanBraceBlock(string source, string startPattern)
{
  int startIdx = source.IndexOf(startPattern, StringComparison.Ordinal);
  if (startIdx < 0)
    return source;

  int ifIdx = source.LastIndexOf("if", startIdx, StringComparison.Ordinal);
  if (ifIdx >= 0 && startIdx - ifIdx < 15)
    startIdx = ifIdx;

  int braceIdx = source.IndexOf("{", startIdx, StringComparison.Ordinal);
  if (braceIdx < 0)
    return source;

  int level = 1;
  int scanIdx = braceIdx + 1;
  while (scanIdx < source.Length && level > 0)
  {
    char c = source[scanIdx];
    if (c == '{')
      level++;
    else if (c == '}')
      level--;
    scanIdx++;
  }

  if (level == 0)
  {
    int endIdx = scanIdx;
    while (endIdx < source.Length && (source[endIdx] == '\r' || source[endIdx] == '\n' || source[endIdx] == ' '))
    {
      endIdx++;
    }
    return source.Substring(0, startIdx) + source.Substring(endIdx);
  }

  return source;
}

string TennaCleanAllBraceBlocks(string source, string startPattern)
{
  string current = source;
  while (true)
  {
    string cleaned = TennaCleanBraceBlock(current, startPattern);
    if (cleaned == current)
      break;
    current = cleaned;
  }
  return current;
}


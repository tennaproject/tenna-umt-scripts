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
if (Data.Scripts.ByName("scr_tenna_config_set_plot_watcher_visible")?.Code is not UndertaleCode)
{
  ScriptError("Tenna Core needs to be updated before installing Plot Watcher.\n\nPlease run GameCore.csx first.");
  return;
}

bool plotWatcherAlreadyInstalled = checkCreate.Contains("_tenna_pw_enabled");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
  ThrowOnNoOpFindReplace = true
};

string createInit = @"
// TENNA_PLOT_WATCHER_CREATE_BEGIN
_tenna_pw_enabled = true;
_tenna_pw_visible = global._tenna_config_ui_plot_watcher_visible;
_tenna_pw_shadow = global.plot;
_tenna_pw_notify_msg = """";
_tenna_pw_notify_timer = 0;
// TENNA_PLOT_WATCHER_CREATE_END
";

string stepCheck = @"
// TENNA_PLOT_WATCHER_STEP_BEGIN
if (keyboard_check_pressed(ord(""3"")) && keyboard_check(vk_alt))
{
    _tenna_pw_visible = !_tenna_pw_visible;
    scr_tenna_config_set_plot_watcher_visible(_tenna_pw_visible);
}

if (_tenna_pw_enabled)
{
    if (global.plot != _tenna_pw_shadow)
    {
        var _tenna_pw_old = _tenna_pw_shadow;
        var _tenna_pw_new = global.plot;
        _tenna_pw_shadow = _tenna_pw_new;
        
        _tenna_pw_notify_msg = ""Plot: "" + string(_tenna_pw_old) + "" -> "" + string(_tenna_pw_new);
        _tenna_pw_notify_timer = 180;
        
        scr_tenna_log(""PlotWatcher"", string(_tenna_pw_old) + "" -> "" + string(_tenna_pw_new));
    }
    
    if (_tenna_pw_notify_timer > 0)
        _tenna_pw_notify_timer--;
}
// TENNA_PLOT_WATCHER_STEP_END
";

string drawDisplay = @"
// TENNA_PLOT_WATCHER_DRAW_BEGIN
if (_tenna_pw_visible)
{
    draw_set_font(fnt_main);
    draw_set_halign(fa_left);
    
    var _tenna_pw_text = ""Plot: "" + string(global.plot);
    draw_set_color(c_black);
    draw_text(9, 9, _tenna_pw_text);
    draw_set_color(c_lime);
    draw_text(8, 8, _tenna_pw_text);
    
    if (_tenna_pw_notify_timer > 0)
    {
        var _tenna_pw_alpha = _tenna_pw_notify_timer / 180;
        draw_set_alpha(_tenna_pw_alpha);
        draw_set_color(c_black);
        draw_text(9, 23, _tenna_pw_notify_msg);
        draw_set_color(c_red);
        draw_text(8, 22, _tenna_pw_notify_msg);
        draw_set_alpha(1);
    }
    
    draw_set_color(c_white);
}
// TENNA_PLOT_WATCHER_DRAW_END
";

try
{
  string currentStepText = GetDecompiledText(stepCode);
  string currentDrawText = GetDecompiledText(drawCode);
  string currentCreateText = GetDecompiledText(createCode);

  string cleanCreate = TennaCleanAllBlocks(currentCreateText, "_tenna_pw_enabled = true;", "_tenna_pw_notify_timer = 0;");
  importGroup.QueueReplace(createCode, cleanCreate + createInit);

  string cleanStep = TennaCleanAllBlocks(currentStepText, "keyboard_check_pressed(ord(\"3\"))", "_tenna_pw_notify_timer--;");
  importGroup.QueueReplace(stepCode, stepCheck + cleanStep);

  string cleanDraw = TennaCleanAllBraceBlocks(currentDrawText, "_tenna_pw_visible");
  importGroup.QueueReplace(drawCode, cleanDraw + drawDisplay);
  
  importGroup.Import();
  if (Environment.GetEnvironmentVariable("TENNA_UMT_SUPPRESS_SCRIPT_MESSAGES") != "1")
    ScriptMessage("Plot Watcher " + (plotWatcherAlreadyInstalled ? "updated" : "installed") + "!\n\nAlt+3 to toggle display.");
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

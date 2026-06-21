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

bool flagWatcherAlreadyInstalled = checkCreate.Contains("_tenna_fw_enabled");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
  ThrowOnNoOpFindReplace = true
};

string createInit = @"
_tenna_fw_max_log = 30;
_tenna_fw_enabled = true;
_tenna_fw_visible = true;
directory_create(""tenna"");
directory_create(""tenna/flag-logs"");
global._tenna_fw_export_filename = ""tenna/flag-logs/flags-"" + global._tenna_core_ts + "".jsonl"";
for (var _tenna_fw_i = 0; _tenna_fw_i < 2000; _tenna_fw_i++)
    _tenna_fw_shadow[_tenna_fw_i] = global.flag[_tenna_fw_i];

for (var _tenna_fw_i = 0; _tenna_fw_i < _tenna_fw_max_log; _tenna_fw_i++)
{
    _tenna_fw_log[_tenna_fw_i] = """";
    _tenna_fw_alpha[_tenna_fw_i] = 0;
}
";

string stepCheck = @"
if (keyboard_check_pressed(ord(""2"")) && keyboard_check(vk_alt))
    _tenna_fw_visible = !_tenna_fw_visible;

if (_tenna_fw_enabled)
{
    for (var _tenna_fw_i = 0; _tenna_fw_i < 2000; _tenna_fw_i++)
    {
        if (_tenna_fw_i == 21 || _tenna_fw_i == 33)
            continue;

        if (global.flag[_tenna_fw_i] != _tenna_fw_shadow[_tenna_fw_i])
        {
            var _tenna_fw_old = _tenna_fw_shadow[_tenna_fw_i];
            var _tenna_fw_new = global.flag[_tenna_fw_i];
            _tenna_fw_shadow[_tenna_fw_i] = _tenna_fw_new;

            for (var _tenna_fw_j = _tenna_fw_max_log - 1; _tenna_fw_j > 0; _tenna_fw_j--)
            {
                _tenna_fw_log[_tenna_fw_j] = _tenna_fw_log[_tenna_fw_j - 1];
                _tenna_fw_alpha[_tenna_fw_j] = _tenna_fw_alpha[_tenna_fw_j - 1];
            }

            _tenna_fw_log[0] = ""Flag["" + string(_tenna_fw_i) + ""]: "" + string(_tenna_fw_old) + "" -> "" + string(_tenna_fw_new);
            _tenna_fw_alpha[0] = 1;

            var _tenna_fw_room = -1;
            if (variable_global_exists(""currentroom""))
                _tenna_fw_room = global.currentroom;
            var _tenna_fw_plot = -1;
            if (variable_global_exists(""plot""))
                _tenna_fw_plot = global.plot;
            var _tenna_fw_chapter = -1;
            if (variable_global_exists(""chapter""))
                _tenna_fw_chapter = global.chapter;

            var _tenna_fw_elapsed = (current_time - global._tenna_core_start_time) / 1000;
            var _tenna_fw_q = chr(34);
            var _tenna_fw_file = file_text_open_append(global._tenna_fw_export_filename);
            file_text_write_string(_tenna_fw_file, ""{"" + _tenna_fw_q + ""elapsedSeconds"" + _tenna_fw_q + "":"" + string(_tenna_fw_elapsed));
            file_text_write_string(_tenna_fw_file, "","" + _tenna_fw_q + ""flagId"" + _tenna_fw_q + "":"" + string(_tenna_fw_i));
            file_text_write_string(_tenna_fw_file, "","" + _tenna_fw_q + ""oldValue"" + _tenna_fw_q + "":"" + string(_tenna_fw_old));
            file_text_write_string(_tenna_fw_file, "","" + _tenna_fw_q + ""newValue"" + _tenna_fw_q + "":"" + string(_tenna_fw_new));
            file_text_write_string(_tenna_fw_file, "","" + _tenna_fw_q + ""chapter"" + _tenna_fw_q + "":"" + string(_tenna_fw_chapter));
            file_text_write_string(_tenna_fw_file, "","" + _tenna_fw_q + ""room"" + _tenna_fw_q + "":"" + string(_tenna_fw_room));
            file_text_write_string(_tenna_fw_file, "","" + _tenna_fw_q + ""plot"" + _tenna_fw_q + "":"" + string(_tenna_fw_plot) + ""}"");
            file_text_writeln(_tenna_fw_file);
            file_text_close(_tenna_fw_file);

            scr_tenna_log(""FlagWatcher"", ""["" + string(_tenna_fw_i) + ""]: "" + string(_tenna_fw_old) + "" -> "" + string(_tenna_fw_new) + "" room="" + string(_tenna_fw_room) + "" plot="" + string(_tenna_fw_plot));
        }
    }

    for (var _tenna_fw_i = 0; _tenna_fw_i < _tenna_fw_max_log; _tenna_fw_i++)
    {
        if (_tenna_fw_alpha[_tenna_fw_i] > 0)
            _tenna_fw_alpha[_tenna_fw_i] -= 0.003;
    }
}
";

string drawDisplay = @"
if (_tenna_fw_visible)
{
    draw_set_font(fnt_main);
    draw_set_halign(fa_right);
    var _tenna_fw_yoff = 8;
    for (var _tenna_fw_i = 0; _tenna_fw_i < _tenna_fw_max_log; _tenna_fw_i++)
    {
        if (_tenna_fw_log[_tenna_fw_i] != """" && _tenna_fw_alpha[_tenna_fw_i] > 0)
        {
            draw_set_alpha(_tenna_fw_alpha[_tenna_fw_i]);
            draw_set_color(c_black);
            draw_text(633, _tenna_fw_yoff + 1, _tenna_fw_log[_tenna_fw_i]);
            draw_set_color(c_yellow);
            draw_text(632, _tenna_fw_yoff, _tenna_fw_log[_tenna_fw_i]);
            _tenna_fw_yoff += 14;
        }
    }
    draw_set_alpha(1);
    draw_set_halign(fa_left);
    draw_set_color(c_white);
}
";

try
{
  if (!flagWatcherAlreadyInstalled)
  {
    importGroup.QueueReplace(createCode, GetDecompiledText(createCode) + createInit);
    importGroup.QueueReplace(stepCode, GetDecompiledText(stepCode) + stepCheck);
    importGroup.QueueReplace(drawCode, GetDecompiledText(drawCode) + drawDisplay);
  }

  importGroup.Import();
  if (Environment.GetEnvironmentVariable("TENNA_UMT_SUPPRESS_SCRIPT_MESSAGES") != "1")
    ScriptMessage("Flag Watcher " + (flagWatcherAlreadyInstalled ? "updated" : "installed") + "!\n\nAlt+2 to toggle display.\nFlag changes export to tenna/flag-logs/.");
}
catch (Exception ex)
{
  ScriptError($"Failed to install: {ex.Message}");
}

﻿using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FastJobSwitcher
{
  public sealed class FastJobSwitcherPlugin : IDalamudPlugin
  {
    public string Name => "Fast Job Switcher";

    private const string commandName = "/fjs";

    public DalamudPluginInterface PluginInterface { get; init; }
    public ICommandManager CommandManager { get; init; }
    public ConfigurationMKI Configuration { get; init; }
    public WindowSystem WindowSystem { get; init; }
    public FastJobSwitcherUI Window { get; init; }

    public FastJobSwitcher Switcher { get; init; }

    public FastJobSwitcherPlugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ICommandManager commandManager)
    {
      pluginInterface.Create<Service>();

      PluginInterface = pluginInterface;
      CommandManager = commandManager;

      WindowSystem = new("FastJobSwitcherPlugin");

      Configuration = LoadConfiguration();
      Configuration.Initialize(SaveConfiguration);

      Window = new FastJobSwitcherUI(Configuration)
      {
        IsOpen = Configuration.IsVisible
      };

      WindowSystem.AddWindow(Window);

      CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
      {
        HelpMessage = "opens the configuration window"
      });

      PluginInterface.UiBuilder.Draw += DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

      Switcher = new FastJobSwitcher(Configuration);
    }

    public void Dispose()
    {
      Switcher.Dispose();

      PluginInterface.UiBuilder.Draw -= DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;

      CommandManager.RemoveHandler(commandName);

      WindowSystem.RemoveAllWindows();
    }

    private ConfigurationMKI LoadConfiguration()
    {
      JObject? baseConfig = null;
      if (File.Exists(PluginInterface.ConfigFile.FullName))
      {
        var configJson = File.ReadAllText(PluginInterface.ConfigFile.FullName);
        baseConfig = JObject.Parse(configJson);
      }

      if (baseConfig != null)
      {
        if ((int?)baseConfig["Version"] == 0)
        {
          var configmki = baseConfig.ToObject<ConfigurationMKI>();
          if (configmki != null)
          {
            return configmki;
          }
        }
      }

      return new ConfigurationMKI();
    }

    public void SaveConfiguration()
    {
      var configJson = JsonConvert.SerializeObject(Configuration, Formatting.Indented);
      File.WriteAllText(PluginInterface.ConfigFile.FullName, configJson);
      if (Switcher != null)
      {
        Switcher.UnRegister();
        Switcher.Register();
      }
    }

    private void SetVisible(bool isVisible)
    {
      Configuration.IsVisible = isVisible;
      Configuration.Save();

      Window.IsOpen = Configuration.IsVisible;
    }

    private void OnCommand(string command, string args)
    {
      SetVisible(!Configuration.IsVisible);
    }

    private void DrawUI()
    {
      WindowSystem.Draw();
    }

    private void DrawConfigUI()
    {
      SetVisible(!Configuration.IsVisible);
    }
  }
}

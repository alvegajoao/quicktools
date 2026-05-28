using System.Globalization;
using QuickTools.Helpers;

namespace QuickTools.Services;

public sealed class LocalizationService : ObservableObject
{
    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        ["Nav_Dashboard"] = "Dashboard",
        ["Nav_AutoClicker"] = "Auto Clicker",
        ["Nav_QuickToggle"] = "Quick Toggle",
        ["Nav_PowerScheduler"] = "Power Scheduler",
        ["Nav_PowerModes"] = "Power Modes",
        ["Nav_Settings"] = "Settings",

        ["Main_Subtitle"] = "Small Windows helpers",
        ["Main_TrayStillRunningTitle"] = "QuickTools is still running",
        ["Main_TrayStillRunningMessage"] = "Use the tray icon to open quick actions or restore the app.",
        ["Main_OpenQuickTools"] = "Open QuickTools",
        ["Main_StartAutoClicker"] = "Start Auto Clicker",
        ["Main_StopAutoClicker"] = "Stop Auto Clicker",
        ["Main_EnableQuickToggle"] = "Enable Quick Toggle",
        ["Main_DisableQuickToggle"] = "Disable Quick Toggle",
        ["Main_OpenQuickToggleWheel"] = "Open Quick Toggle Wheel",
        ["Main_PowerPrefix"] = "Power: {0}",
        ["Main_PauseScheduledPowerEvents"] = "Pause scheduled power events",
        ["Main_ExitQuickTools"] = "Exit QuickTools",
        ["Main_HotkeyDialogTitle"] = "QuickTools hotkey",
        ["Main_QuickToggleHotkeyError"] = "Could not register Quick Toggle hotkey",
        ["Main_QuickToggleOpenWheel"] = "Press {0} to open wheel",
        ["Main_WheelDisabled"] = "Wheel is disabled",

        ["Common_OK"] = "OK",
        ["Common_Hotkey"] = "Hotkey: {0}",
        ["Common_Loading"] = "Loading...",
        ["Common_Unknown"] = "Unknown",
        ["Common_None"] = "None",
        ["Common_Unavailable"] = "Unavailable",
        ["Common_Ready"] = "Ready",
        ["Common_Enabled"] = "Enabled",
        ["Common_Disabled"] = "Disabled",
        ["Common_Active"] = "Active",
        ["Common_Available"] = "Available",
        ["Common_Save"] = "Save",
        ["Common_Refresh"] = "Refresh",

        ["Dashboard_Title"] = "Dashboard",
        ["Dashboard_Subtitle"] = "Quick overview of what is active right now.",
        ["Dashboard_AutoClicker"] = "Auto Clicker",
        ["Dashboard_Speed"] = "Speed",
        ["Dashboard_QuickToggle"] = "Quick Toggle",
        ["Dashboard_PowerMode"] = "Power Mode",
        ["Dashboard_CurrentWindowsMode"] = "Current Windows mode",
        ["Dashboard_CouldNotReadPowerCfg"] = "Could not read powercfg",
        ["Dashboard_PowerScheduler"] = "Power Scheduler",
        ["Dashboard_PauseAllEvents"] = "Pause all events",
        ["Dashboard_AutoClickerSummaryActive"] = "{0} active",
        ["Dashboard_AutoClickerSummarySpeed"] = "{0}% speed",
        ["Dashboard_QuickToggleSummaryEnabled"] = "{0}/8 actions on {1}",
        ["Dashboard_QuickToggleSummaryDisabled"] = "Off - {0}/8 actions set",
        ["Dashboard_NextScheduledAction"] = "{0} at {1} ({2})",
        ["Dashboard_NoActiveEvents"] = "No active events",
        ["Dashboard_AllEventsPaused"] = "All events paused",
        ["Dashboard_OneActiveEvent"] = "1 active event",
        ["Dashboard_ManyActiveEvents"] = "{0} active events",
        ["Dashboard_PausedCount"] = "{0} paused",
        ["Dashboard_SystemPerformance"] = "System performance",
        ["Dashboard_SystemPerformanceSubtitle"] = "Live CPU, GPU, memory, disk and network usage",
        ["Dashboard_Live"] = "Live",
        ["Dashboard_CPU"] = "CPU",
        ["Dashboard_GPU"] = "GPU",
        ["Dashboard_Temperature"] = "Temp {0}",
        ["Dashboard_Memory"] = "Memory",
        ["Dashboard_Disk"] = "Disk",
        ["Dashboard_Download"] = "Download",
        ["Dashboard_Upload"] = "Upload",

        ["AutoClicker_Title"] = "Auto Clicker",
        ["AutoClicker_Subtitle"] = "Fast, visible click automation with safe controls.",
        ["AutoClicker_ActiveTime"] = "Active time: {0}",
        ["AutoClicker_Start"] = "Start",
        ["AutoClicker_Stop"] = "Stop",
        ["AutoClicker_SpeedSection"] = "Speed",
        ["AutoClicker_Slow"] = "Slow",
        ["AutoClicker_Fast"] = "Fast",
        ["AutoClicker_MouseButton"] = "Mouse button",
        ["AutoClicker_Left"] = "Left",
        ["AutoClicker_Primary"] = "Primary",
        ["AutoClicker_Middle"] = "Middle",
        ["AutoClicker_Wheel"] = "Wheel",
        ["AutoClicker_Right"] = "Right",
        ["AutoClicker_Secondary"] = "Secondary",
        ["AutoClicker_ClickType"] = "Click type",
        ["AutoClicker_UseCurrentMousePosition"] = "Use current mouse position",
        ["AutoClicker_ActiveCursor"] = "Active cursor",
        ["AutoClicker_Running"] = "Running",
        ["AutoClicker_Stopped"] = "Stopped",
        ["AutoClicker_Interval"] = "{0} ms between clicks",
        ["AutoClicker_SingleClick"] = "Single click",
        ["AutoClicker_DoubleClick"] = "Double click",
        ["Cursor_Cross"] = "Crosshair",
        ["Cursor_Hand"] = "Hand",
        ["Cursor_Pen"] = "Precision",
        ["Cursor_ScrollAll"] = "Target",
        ["Cursor_SizeAll"] = "Move",
        ["Cursor_Wait"] = "Wait",
        ["Cursor_Help"] = "Help",
        ["Cursor_Arrow"] = "Arrow",

        ["QuickToggle_Title"] = "Quick Toggle",
        ["QuickToggle_Subtitle"] = "Assign system actions to the hotkey wheel for fast access.",
        ["QuickToggle_Enable"] = "Enable",
        ["QuickToggle_Disable"] = "Disable",
        ["QuickToggle_AvailableActions"] = "Available actions",
        ["QuickToggle_MaximumOnWheel"] = "Maximum 8 on wheel",
        ["QuickToggle_AddToWheel"] = "+ Wheel",
        ["QuickToggle_OnWheel"] = "On wheel",
        ["QuickToggle_WheelCount"] = "{0}/8 on wheel",

        ["QuickAction_mute_Name"] = "Mute / Unmute",
        ["QuickAction_mute_Description"] = "Toggle system audio.",
        ["QuickAction_vol_up_Name"] = "Volume +",
        ["QuickAction_vol_up_Description"] = "Increase system volume.",
        ["QuickAction_vol_down_Name"] = "Volume -",
        ["QuickAction_vol_down_Description"] = "Decrease system volume.",
        ["QuickAction_play_pause_Name"] = "Play / Pause",
        ["QuickAction_play_pause_Description"] = "Control current media playback.",
        ["QuickAction_screenshot_Name"] = "Screenshot",
        ["QuickAction_screenshot_Description"] = "Take a screenshot.",
        ["QuickAction_lock_Name"] = "Lock PC",
        ["QuickAction_lock_Description"] = "Lock this Windows session.",
        ["QuickAction_dark_mode_Name"] = "Dark Mode",
        ["QuickAction_dark_mode_Description"] = "Toggle Windows app theme.",
        ["QuickAction_clipboard_Name"] = "Clipboard",
        ["QuickAction_clipboard_Description"] = "Open clipboard history.",
        ["QuickAction_calculator_Name"] = "Calculator",
        ["QuickAction_calculator_Description"] = "Open Calculator.",
        ["QuickAction_taskmgr_Name"] = "Task Manager",
        ["QuickAction_taskmgr_Description"] = "Open Task Manager.",
        ["QuickAction_wifi_Name"] = "Wi-Fi",
        ["QuickAction_wifi_Description"] = "Enable or disable Wi-Fi.",
        ["QuickAction_settings_Name"] = "Settings",
        ["QuickAction_settings_Description"] = "Open Windows Settings.",

        ["PowerScheduler_Title"] = "Power Scheduler",
        ["PowerScheduler_Subtitle"] = "Schedule multiple power events and pause them individually.",
        ["PowerScheduler_PauseAll"] = "Pause all",
        ["PowerScheduler_ScheduleEvent"] = "Schedule event",
        ["PowerScheduler_Action"] = "Action",
        ["PowerScheduler_Hour"] = "Hour",
        ["PowerScheduler_Minute"] = "Minute",
        ["PowerScheduler_Schedule"] = "+ Schedule",
        ["PowerScheduler_ConfirmationNotice"] = "Shutdown and Restart ask for confirmation before scheduling.",
        ["PowerScheduler_NoScheduledEvents"] = "No scheduled events",
        ["PowerScheduler_Pause"] = "Pause",
        ["PowerScheduler_Resume"] = "Resume",
        ["PowerScheduler_NoActiveEvents"] = "No active events",
        ["PowerScheduler_OneActiveEvent"] = "1 active event",
        ["PowerScheduler_ManyActiveEvents"] = "{0} active events",
        ["PowerScheduler_NoUpcomingEvents"] = "No upcoming events",
        ["PowerScheduler_NextEvent"] = "Next: {0} at {1} ({2})",
        ["PowerScheduler_CountActivePaused"] = "{0} active / {1} paused",
        ["PowerScheduler_CountEvents"] = "{0} events",
        ["PowerScheduler_ChooseValidTime"] = "Choose a valid time.",
        ["PowerScheduler_ScheduledFor"] = "{0} scheduled for {1} ({2}).",
        ["PowerScheduler_EventRemoved"] = "Event removed.",
        ["PowerScheduler_EventPaused"] = "Event paused.",
        ["PowerScheduler_CannotResumePassed"] = "Cannot resume because this event time has already passed.",
        ["PowerScheduler_EventResumed"] = "Event resumed.",
        ["PowerScheduler_AllEventsPaused"] = "All events paused.",
        ["PowerScheduler_Today"] = "Today",
        ["PowerScheduler_Tomorrow"] = "Tomorrow",
        ["PowerAction_Shutdown"] = "Shutdown",
        ["PowerAction_Restart"] = "Restart",
        ["PowerAction_Suspend"] = "Suspend",
        ["PowerAction_Hibernate"] = "Hibernate",

        ["PowerModes_Title"] = "Power Modes",
        ["PowerModes_Subtitle"] = "Switch Windows power plans with clean, visible mode tiles.",
        ["PowerModes_CurrentPowerMode"] = "Current power mode",
        ["PowerModes_Selected"] = "Selected",
        ["PowerModes_NoPowerPlansFound"] = "No power plans found.",
        ["PowerModes_CouldNotReadPowerPlans"] = "Could not read power plans: {0}",
        ["PowerModes_PowerPlanChanged"] = "Power plan changed.",
        ["PowerModes_CouldNotChangePowerPlan"] = "Could not change power plan: {0}",
        ["PowerModes_UltimateUnavailable"] = "Ultimate Performance is not available on this system.",
        ["PowerPlan_Balanced"] = "Balanced",
        ["PowerPlan_HighPerformance"] = "High performance",
        ["PowerPlan_PowerSaver"] = "Power saver",
        ["PowerPlan_UltimatePerformance"] = "Ultimate",
        ["PowerPlan_Balanced_Description"] = "Recommended everyday mode.",
        ["PowerPlan_HighPerformance_Description"] = "Prioritizes performance.",
        ["PowerPlan_PowerSaver_Description"] = "Reduces energy usage.",
        ["PowerPlan_UltimatePerformance_Description"] = "Maximum performance mode.",
        ["PowerPlan_Custom_Description"] = "Custom Windows power plan.",
        ["PowerService_PlanUnavailable"] = "Power plan '{0}' is not available on this system.",

        ["Settings_Title"] = "Settings",
        ["Settings_Subtitle"] = "Theme, startup, hotkey and JSON configuration.",
        ["Settings_Application"] = "Application",
        ["Settings_Theme"] = "Theme",
        ["Settings_Language"] = "Language",
        ["Settings_AutoClickerHotkey"] = "Auto Clicker hotkey",
        ["Settings_QuickToggleHotkey"] = "Quick Toggle hotkey",
        ["Settings_StartWithWindows"] = "Start with Windows",
        ["Settings_DataFolder"] = "Data folder",
        ["Settings_SettingsFile"] = "Settings file",
        ["Settings_ExportJson"] = "Export JSON",
        ["Settings_ImportJson"] = "Import JSON",
        ["Settings_SettingsSaved"] = "Settings saved.",
        ["Settings_ExportCopied"] = "Settings JSON copied to clipboard.",
        ["Settings_ClipboardNoJson"] = "Clipboard does not contain JSON.",
        ["Settings_Imported"] = "Settings imported from clipboard.",
        ["Settings_CouldNotSave"] = "Could not save settings: {0}",
        ["Settings_CouldNotImport"] = "Could not import settings: {0}",
        ["Theme_Light"] = "White",
        ["Theme_Dark"] = "Black",
        ["Theme_System"] = "Automatic",
        ["Language_en"] = "English",
        ["Language_pt"] = "Portuguese",

        ["Update_Title"] = "QuickTools update",
        ["Update_Available"] = "A new QuickTools update is available. Install it now?",
        ["Update_Install"] = "Install update",
        ["Update_Later"] = "Later",
        ["Update_Failed"] = "QuickTools could not install the update.\n\n{0}",
        ["Update_ScriptFailed"] = "QuickTools could not finish installing the update.`n`n{0}"
    };

    private static readonly IReadOnlyDictionary<string, string> Portuguese = new Dictionary<string, string>
    {
        ["Nav_Dashboard"] = "Painel",
        ["Nav_AutoClicker"] = "Auto Clicker",
        ["Nav_QuickToggle"] = "Quick Toggle",
        ["Nav_PowerScheduler"] = "Agendador de Energia",
        ["Nav_PowerModes"] = "Modos de Energia",
        ["Nav_Settings"] = "Definições",

        ["Main_Subtitle"] = "Pequenas ferramentas para Windows",
        ["Main_TrayStillRunningTitle"] = "O QuickTools continua em execução",
        ["Main_TrayStillRunningMessage"] = "Use o ícone na bandeja para abrir ações rápidas ou restaurar a aplicação.",
        ["Main_OpenQuickTools"] = "Abrir QuickTools",
        ["Main_StartAutoClicker"] = "Iniciar Auto Clicker",
        ["Main_StopAutoClicker"] = "Parar Auto Clicker",
        ["Main_EnableQuickToggle"] = "Ativar Quick Toggle",
        ["Main_DisableQuickToggle"] = "Desativar Quick Toggle",
        ["Main_OpenQuickToggleWheel"] = "Abrir roda do Quick Toggle",
        ["Main_PowerPrefix"] = "Energia: {0}",
        ["Main_PauseScheduledPowerEvents"] = "Pausar eventos de energia agendados",
        ["Main_ExitQuickTools"] = "Sair do QuickTools",
        ["Main_HotkeyDialogTitle"] = "Atalho do QuickTools",
        ["Main_QuickToggleHotkeyError"] = "Não foi possível registar o atalho do Quick Toggle",
        ["Main_QuickToggleOpenWheel"] = "Prima {0} para abrir a roda",
        ["Main_WheelDisabled"] = "A roda está desativada",

        ["Common_OK"] = "OK",
        ["Common_Hotkey"] = "Atalho: {0}",
        ["Common_Loading"] = "A carregar...",
        ["Common_Unknown"] = "Desconhecido",
        ["Common_None"] = "Nenhum",
        ["Common_Unavailable"] = "Indisponível",
        ["Common_Ready"] = "Pronto",
        ["Common_Enabled"] = "Ativado",
        ["Common_Disabled"] = "Desativado",
        ["Common_Active"] = "Ativo",
        ["Common_Available"] = "Disponível",
        ["Common_Save"] = "Guardar",
        ["Common_Refresh"] = "Atualizar",

        ["Dashboard_Title"] = "Painel",
        ["Dashboard_Subtitle"] = "Resumo rápido do que está ativo neste momento.",
        ["Dashboard_AutoClicker"] = "Auto Clicker",
        ["Dashboard_Speed"] = "Velocidade",
        ["Dashboard_QuickToggle"] = "Quick Toggle",
        ["Dashboard_PowerMode"] = "Modo de Energia",
        ["Dashboard_CurrentWindowsMode"] = "Modo atual do Windows",
        ["Dashboard_CouldNotReadPowerCfg"] = "Não foi possível ler o powercfg",
        ["Dashboard_PowerScheduler"] = "Agendador de Energia",
        ["Dashboard_PauseAllEvents"] = "Pausar todos os eventos",
        ["Dashboard_AutoClickerSummaryActive"] = "{0} ativo",
        ["Dashboard_AutoClickerSummarySpeed"] = "{0}% de velocidade",
        ["Dashboard_QuickToggleSummaryEnabled"] = "{0}/8 ações em {1}",
        ["Dashboard_QuickToggleSummaryDisabled"] = "Desligado - {0}/8 ações definidas",
        ["Dashboard_NextScheduledAction"] = "{0} às {1} ({2})",
        ["Dashboard_NoActiveEvents"] = "Sem eventos ativos",
        ["Dashboard_AllEventsPaused"] = "Todos os eventos estão em pausa",
        ["Dashboard_OneActiveEvent"] = "1 evento ativo",
        ["Dashboard_ManyActiveEvents"] = "{0} eventos ativos",
        ["Dashboard_PausedCount"] = "{0} em pausa",
        ["Dashboard_SystemPerformance"] = "Desempenho do sistema",
        ["Dashboard_SystemPerformanceSubtitle"] = "Utilização em tempo real do CPU, GPU, memória, disco e rede",
        ["Dashboard_Live"] = "Em direto",
        ["Dashboard_CPU"] = "CPU",
        ["Dashboard_GPU"] = "GPU",
        ["Dashboard_Temperature"] = "Temp {0}",
        ["Dashboard_Memory"] = "Memória",
        ["Dashboard_Disk"] = "Disco",
        ["Dashboard_Download"] = "Download",
        ["Dashboard_Upload"] = "Upload",

        ["AutoClicker_Title"] = "Auto Clicker",
        ["AutoClicker_Subtitle"] = "Automação de cliques rápida, visível e com controlo seguro.",
        ["AutoClicker_ActiveTime"] = "Tempo ativo: {0}",
        ["AutoClicker_Start"] = "Iniciar",
        ["AutoClicker_Stop"] = "Parar",
        ["AutoClicker_SpeedSection"] = "Velocidade",
        ["AutoClicker_Slow"] = "Lento",
        ["AutoClicker_Fast"] = "Rápido",
        ["AutoClicker_MouseButton"] = "Botão do rato",
        ["AutoClicker_Left"] = "Esquerdo",
        ["AutoClicker_Primary"] = "Principal",
        ["AutoClicker_Middle"] = "Meio",
        ["AutoClicker_Wheel"] = "Roda",
        ["AutoClicker_Right"] = "Direito",
        ["AutoClicker_Secondary"] = "Secundário",
        ["AutoClicker_ClickType"] = "Tipo de clique",
        ["AutoClicker_UseCurrentMousePosition"] = "Usar a posição atual do rato",
        ["AutoClicker_ActiveCursor"] = "Cursor ativo",
        ["AutoClicker_Running"] = "Em execução",
        ["AutoClicker_Stopped"] = "Parado",
        ["AutoClicker_Interval"] = "{0} ms entre cliques",
        ["AutoClicker_SingleClick"] = "Clique simples",
        ["AutoClicker_DoubleClick"] = "Clique duplo",
        ["Cursor_Cross"] = "Mira",
        ["Cursor_Hand"] = "Mão",
        ["Cursor_Pen"] = "Precisão",
        ["Cursor_ScrollAll"] = "Alvo",
        ["Cursor_SizeAll"] = "Mover",
        ["Cursor_Wait"] = "Aguarde",
        ["Cursor_Help"] = "Ajuda",
        ["Cursor_Arrow"] = "Seta",

        ["QuickToggle_Title"] = "Quick Toggle",
        ["QuickToggle_Subtitle"] = "Associe ações do sistema à roda do atalho para acesso rápido.",
        ["QuickToggle_Enable"] = "Ativar",
        ["QuickToggle_Disable"] = "Desativar",
        ["QuickToggle_AvailableActions"] = "Ações disponíveis",
        ["QuickToggle_MaximumOnWheel"] = "Máximo de 8 na roda",
        ["QuickToggle_AddToWheel"] = "+ Roda",
        ["QuickToggle_OnWheel"] = "Na roda",
        ["QuickToggle_WheelCount"] = "{0}/8 na roda",

        ["QuickAction_mute_Name"] = "Som ligar / desligar",
        ["QuickAction_mute_Description"] = "Alternar o áudio do sistema.",
        ["QuickAction_vol_up_Name"] = "Volume +",
        ["QuickAction_vol_up_Description"] = "Aumentar o volume do sistema.",
        ["QuickAction_vol_down_Name"] = "Volume -",
        ["QuickAction_vol_down_Description"] = "Diminuir o volume do sistema.",
        ["QuickAction_play_pause_Name"] = "Reproduzir / Pausar",
        ["QuickAction_play_pause_Description"] = "Controlar a reprodução multimédia atual.",
        ["QuickAction_screenshot_Name"] = "Captura de ecrã",
        ["QuickAction_screenshot_Description"] = "Tirar uma captura de ecrã.",
        ["QuickAction_lock_Name"] = "Bloquear PC",
        ["QuickAction_lock_Description"] = "Bloquear esta sessão do Windows.",
        ["QuickAction_dark_mode_Name"] = "Modo Escuro",
        ["QuickAction_dark_mode_Description"] = "Alternar o tema das apps do Windows.",
        ["QuickAction_clipboard_Name"] = "Área de transferência",
        ["QuickAction_clipboard_Description"] = "Abrir o histórico da área de transferência.",
        ["QuickAction_calculator_Name"] = "Calculadora",
        ["QuickAction_calculator_Description"] = "Abrir a Calculadora.",
        ["QuickAction_taskmgr_Name"] = "Gestor de Tarefas",
        ["QuickAction_taskmgr_Description"] = "Abrir o Gestor de Tarefas.",
        ["QuickAction_wifi_Name"] = "Wi-Fi",
        ["QuickAction_wifi_Description"] = "Ativar ou desativar o Wi-Fi.",
        ["QuickAction_settings_Name"] = "Definições",
        ["QuickAction_settings_Description"] = "Abrir as Definições do Windows.",

        ["PowerScheduler_Title"] = "Agendador de Energia",
        ["PowerScheduler_Subtitle"] = "Agende vários eventos de energia e pause-os individualmente.",
        ["PowerScheduler_PauseAll"] = "Pausar tudo",
        ["PowerScheduler_ScheduleEvent"] = "Agendar evento",
        ["PowerScheduler_Action"] = "Ação",
        ["PowerScheduler_Hour"] = "Hora",
        ["PowerScheduler_Minute"] = "Minuto",
        ["PowerScheduler_Schedule"] = "+ Agendar",
        ["PowerScheduler_ConfirmationNotice"] = "Desligar e Reiniciar pedem confirmação antes do agendamento.",
        ["PowerScheduler_NoScheduledEvents"] = "Sem eventos agendados",
        ["PowerScheduler_Pause"] = "Pausar",
        ["PowerScheduler_Resume"] = "Retomar",
        ["PowerScheduler_NoActiveEvents"] = "Sem eventos ativos",
        ["PowerScheduler_OneActiveEvent"] = "1 evento ativo",
        ["PowerScheduler_ManyActiveEvents"] = "{0} eventos ativos",
        ["PowerScheduler_NoUpcomingEvents"] = "Sem próximos eventos",
        ["PowerScheduler_NextEvent"] = "Próximo: {0} às {1} ({2})",
        ["PowerScheduler_CountActivePaused"] = "{0} ativos / {1} em pausa",
        ["PowerScheduler_CountEvents"] = "{0} eventos",
        ["PowerScheduler_ChooseValidTime"] = "Escolha uma hora válida.",
        ["PowerScheduler_ScheduledFor"] = "{0} agendado para as {1} ({2}).",
        ["PowerScheduler_EventRemoved"] = "Evento removido.",
        ["PowerScheduler_EventPaused"] = "Evento em pausa.",
        ["PowerScheduler_CannotResumePassed"] = "Não é possível retomar porque a hora deste evento já passou.",
        ["PowerScheduler_EventResumed"] = "Evento retomado.",
        ["PowerScheduler_AllEventsPaused"] = "Todos os eventos estão em pausa.",
        ["PowerScheduler_Today"] = "Hoje",
        ["PowerScheduler_Tomorrow"] = "Amanhã",
        ["PowerAction_Shutdown"] = "Desligar",
        ["PowerAction_Restart"] = "Reiniciar",
        ["PowerAction_Suspend"] = "Suspender",
        ["PowerAction_Hibernate"] = "Hibernar",

        ["PowerModes_Title"] = "Modos de Energia",
        ["PowerModes_Subtitle"] = "Mude os planos de energia do Windows com cartões visíveis e limpos.",
        ["PowerModes_CurrentPowerMode"] = "Modo de energia atual",
        ["PowerModes_Selected"] = "Selecionado",
        ["PowerModes_NoPowerPlansFound"] = "Não foram encontrados planos de energia.",
        ["PowerModes_CouldNotReadPowerPlans"] = "Não foi possível ler os planos de energia: {0}",
        ["PowerModes_PowerPlanChanged"] = "Plano de energia alterado.",
        ["PowerModes_CouldNotChangePowerPlan"] = "Não foi possível alterar o plano de energia: {0}",
        ["PowerModes_UltimateUnavailable"] = "O modo Desempenho Máximo não está disponível neste sistema.",
        ["PowerPlan_Balanced"] = "Equilibrado",
        ["PowerPlan_HighPerformance"] = "Alto desempenho",
        ["PowerPlan_PowerSaver"] = "Poupança de energia",
        ["PowerPlan_UltimatePerformance"] = "Máximo",
        ["PowerPlan_Balanced_Description"] = "Modo recomendado para o dia a dia.",
        ["PowerPlan_HighPerformance_Description"] = "Prioriza o desempenho.",
        ["PowerPlan_PowerSaver_Description"] = "Reduz o consumo de energia.",
        ["PowerPlan_UltimatePerformance_Description"] = "Modo de desempenho máximo.",
        ["PowerPlan_Custom_Description"] = "Plano de energia personalizado do Windows.",
        ["PowerService_PlanUnavailable"] = "O plano de energia '{0}' não está disponível neste sistema.",

        ["Settings_Title"] = "Definições",
        ["Settings_Subtitle"] = "Tema, arranque, atalhos e configuração em JSON.",
        ["Settings_Application"] = "Aplicação",
        ["Settings_Theme"] = "Tema",
        ["Settings_Language"] = "Idioma",
        ["Settings_AutoClickerHotkey"] = "Atalho do Auto Clicker",
        ["Settings_QuickToggleHotkey"] = "Atalho do Quick Toggle",
        ["Settings_StartWithWindows"] = "Iniciar com o Windows",
        ["Settings_DataFolder"] = "Pasta de dados",
        ["Settings_SettingsFile"] = "Ficheiro de definições",
        ["Settings_ExportJson"] = "Exportar JSON",
        ["Settings_ImportJson"] = "Importar JSON",
        ["Settings_SettingsSaved"] = "Definições guardadas.",
        ["Settings_ExportCopied"] = "JSON das definições copiado para a área de transferência.",
        ["Settings_ClipboardNoJson"] = "A área de transferência não contém JSON.",
        ["Settings_Imported"] = "Definições importadas da área de transferência.",
        ["Settings_CouldNotSave"] = "Não foi possível guardar as definições: {0}",
        ["Settings_CouldNotImport"] = "Não foi possível importar as definições: {0}",
        ["Theme_Light"] = "Branco",
        ["Theme_Dark"] = "Preto",
        ["Theme_System"] = "Automático",
        ["Language_en"] = "Inglês",
        ["Language_pt"] = "Português",

        ["Update_Title"] = "Atualização do QuickTools",
        ["Update_Available"] = "Está disponível uma nova atualização do QuickTools. Pretende instalá-la agora?",
        ["Update_Install"] = "Instalar atualização",
        ["Update_Later"] = "Mais tarde",
        ["Update_Failed"] = "O QuickTools não conseguiu instalar a atualização.\n\n{0}",
        ["Update_ScriptFailed"] = "O QuickTools não conseguiu concluir a instalação da atualização.`n`n{0}"
    };

    private string _currentLanguage = "en";

    private LocalizationService()
    {
    }

    public static LocalizationService Instance { get; } = new();

    public string CurrentLanguage
    {
        get => _currentLanguage;
        private set => SetProperty(ref _currentLanguage, value);
    }

    public CultureInfo Culture => CurrentLanguage == "pt"
        ? new CultureInfo("pt-PT")
        : new CultureInfo("en-US");

    public string this[string key] => Get(key);

    public event EventHandler? LanguageChanged;

    public void SetLanguage(string? language)
    {
        var normalized = NormalizeLanguage(language);
        if (CurrentLanguage == normalized)
        {
            return;
        }

        CurrentLanguage = normalized;
        OnPropertyChanged("Item[]");
        OnPropertyChanged(nameof(Culture));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Get(string key)
    {
        var dictionary = CurrentLanguage == "pt" ? Portuguese : English;
        if (dictionary.TryGetValue(key, out var value))
        {
            return value;
        }

        return English.TryGetValue(key, out var fallback) ? fallback : key;
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, Get(key), args);
    }

    public string TranslatePowerAction(string action)
    {
        return Get($"PowerAction_{action}");
    }

    public string TranslatePowerPlanKind(string kind)
    {
        return kind switch
        {
            "Balanced" => Get("PowerPlan_Balanced"),
            "HighPerformance" => Get("PowerPlan_HighPerformance"),
            "PowerSaver" => Get("PowerPlan_PowerSaver"),
            "UltimatePerformance" => Get("PowerPlan_UltimatePerformance"),
            _ => kind
        };
    }

    public static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en";
        }

        return language.StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? "pt" : "en";
    }
}

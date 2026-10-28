using Godot;

// Управляет состоянием текущего игрового поля.
public partial class GameManager : Node3D
{
    public static GameManager Instance { get; private set; }

    public enum GameState // Состояние игры
    {
        Playing,
        Lost
    }

    public GameState State { get; private set; } = GameState.Playing;

    [Export]
    private Node3D _core;

    [Export]
    private Control _gameOverPanel;

    [Export]
    private Label _messageLabel;

    [Export]
    private Button _restartButton;
    private HealthComponent _coreHealth;

    public override void _EnterTree()
    {
        Instance = this;

        // В том числе сбрасывает паузу при повторном запуске сцены.
        GetTree().Paused = false;
    }

    public override void _Ready()
    {
        // Проверяем, что все необходимые узлы назначены в Inspector.
        if (_core == null
            || _gameOverPanel == null
            || _messageLabel == null
            || _restartButton == null)
        {
            GD.Print("GameManager: назначь Core и узлы экрана поражения в Inspector.");
            return;
        }

        _coreHealth =
            _core.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (_coreHealth == null)
        {
            GD.PushError("GameManager: у ядра нет HealthComponent.");
            return;
        }
        // Изначально скрываем панель поражения
        State = GameState.Playing;
        _gameOverPanel.Hide();

        _coreHealth.Died += OnCoreDied; // добавляем обработчик события смерти ядра
        _restartButton.Pressed += RestartGame;
    }

    private void OnCoreDied() // Если ядро умерло, показываем панель поражения
    {
        EndGame();
    }

    public void EndGame()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        State = GameState.Lost;

        _messageLabel.Text = "Ядро уничтожено";
        _gameOverPanel.Show();

        GetTree().Paused = true;
    }

    public void RestartGame() // Перезапускает текущую сцену
    {
        GetTree().Paused = false;

        Error result = GetTree().ReloadCurrentScene();

        if (result != Error.Ok)
        {
            GD.PushError($"Не удалось перезапустить сцену: {result}");
        }
    }

    public override void _ExitTree()  // Удаляем обработчики событий при выходе из дерева сцены
    {
        if (GodotObject.IsInstanceValid(_coreHealth))
        {
            _coreHealth.Died -= OnCoreDied;
        }

        if (GodotObject.IsInstanceValid(_restartButton))
        {
            _restartButton.Pressed -= RestartGame;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
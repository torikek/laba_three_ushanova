using Godot;

public partial class EnemySpawner : Node3D
{
    [Export]
    private PackedScene _enemyTowerScene =
        GD.Load<PackedScene>("res://scenes/EnemyTower.tscn");

    [Export]
    private PackedScene _crawlerScene =
        GD.Load<PackedScene>("res://scenes/Crawler.tscn");

    [Export]
    private int _maxAdditionalWaves = 1;

    private int _spawnedWaves;

    private Timer _spawnTimer;
    private Node3D _towerPoints;
    private Node3D _crawlerPoints;

    public override void _Ready()
    {
        _spawnTimer = GetNode<Timer>("SpawnTimer");
        _towerPoints = GetNode<Node3D>("TowerPoints");
        _crawlerPoints = GetNode<Node3D>("CrawlerPoints");

        if (_enemyTowerScene == null || _crawlerScene == null)
        {
            GD.PushError("EnemySpawner: не назначены сцены врагов.");
            return;
        }

        _spawnedWaves = 0;

        _spawnTimer.WaitTime = 10.0;
        _spawnTimer.OneShot = false;
        _spawnTimer.Timeout += SpawnWave;

        // Начальные враги уже стоят на карте.
        // Первую дополнительную волну создаём через 10 секунд.
        if (_maxAdditionalWaves > 0)
        {
            _spawnTimer.Start();
        }
    }
    public override void _Process(double delta)
    {
        GameManager manager = GameManager.Instance;

        if (!GodotObject.IsInstanceValid(manager)
            || manager.State != GameManager.GameState.Playing)
        {
            return;
        }

        // Пока остались будущие волны, победить нельзя.
        if (_spawnedWaves < _maxAdditionalWaves)
        {
            return;
        }

        // Все волны созданы, но оставшихся врагов ещё нужно уничтожить.
        if (GetTree().GetNodesInGroup("enemies").Count > 0)
        {
            return;
        }

        manager.WinGame();
    }

    private void SpawnWave()
    {
        if (_spawnedWaves >= _maxAdditionalWaves
            || (GodotObject.IsInstanceValid(GameManager.Instance)
                && GameManager.Instance.State != GameManager.GameState.Playing))
        {
            _spawnTimer.Stop();
            return;
        }

        SpawnAtPoints(_enemyTowerScene, _towerPoints);
        SpawnAtPoints(_crawlerScene, _crawlerPoints);

        _spawnedWaves++;

        // После второй дополнительной волны прекращаем спавн.
        if (_spawnedWaves >= _maxAdditionalWaves)
        {
            _spawnTimer.Stop();
        }
    }

    private void SpawnAtPoints(PackedScene scene, Node3D points)
    {
        Node3D world = GetParent<Node3D>();

        foreach (Node child in points.GetChildren())
        {
            if (child is not Marker3D point)
            {
                continue;
            }

            Node3D enemy = scene.Instantiate<Node3D>();

            enemy.Transform =
                world.GlobalTransform.AffineInverse()
                * point.GlobalTransform;

            world.AddChild(enemy);
        }
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_spawnTimer))
        {
            _spawnTimer.Timeout -= SpawnWave;
        }
    }
}
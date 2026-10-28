using Godot;
using System.Collections.Generic;

// Управляет выбором, предпросмотром и установкой башен перед игроком.
[GlobalClass]
public partial class BuildComponent : Node
{
    // Порядок сцен в обоих массивах должен совпадать с клавишами 1, 2, 3.
    [Export]
    private Godot.Collections.Array<PackedScene> _towerScenes = new()
    {
        GD.Load<PackedScene>("res://scenes/Tower.tscn"),
        GD.Load<PackedScene>("res://scenes/FreezeTower.tscn"),
        GD.Load<PackedScene>("res://scenes/BoomTower.tscn")
    };

    [Export]
    private Godot.Collections.Array<PackedScene> _previewScenes = new()
    {
        GD.Load<PackedScene>("res://scenes/TowerPreview.tscn"),
        GD.Load<PackedScene>("res://scenes/FreezeTowerPreview.tscn"),
        GD.Load<PackedScene>("res://scenes/BoomTowerPreview.tscn")
    };

    [Export(PropertyHint.Range, "1,10,0.1")]
    private float _buildDistance = 3f;

    [Export]
    private float _groundHeight = 0f;

    // Запас от края пола: должен быть не меньше радиуса основания башни.
    [Export(PropertyHint.Range, "0.1,3,0.05")]
    private float _edgeMargin = 0.65f;

    [Export(PropertyHint.Layers3DPhysics)]
    private uint _blockingMask = 1;

    [Export]
    private MeshInstance3D _floorMesh;

    [Export]
    private Color _blockedColor = new Color(1f, 0.15f, 0.15f, 0.45f);

    private Node3D _player;
    private Node3D _preview;
    private Shape3D _placementShape;
    private Transform3D _shapeTransform;
    private StandardMaterial3D _blockedMaterial;
    private readonly List<MeshInstance3D> _previewMeshes = new();
    private int _selectedTowerIndex;
    private bool _isBuilding;

    public override void _Ready()
    {
        _player = GetParent() as Node3D;
        if (_floorMesh == null)
        {
            _floorMesh = GetTree().CurrentScene
                .GetNodeOrNull<MeshInstance3D>("Floor/MeshInstance3D");
        }

        if (_player == null || _floorMesh == null)
        {
            GD.PushError("BuildComponent: нужен родитель-игрок и меш пола в Inspector.");
            SetPhysicsProcess(false);
            return;
        }

        _blockedMaterial = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = _blockedColor
        };
    }

    // Проверка физического места и подтверждение выполняются в одном такте.
    public override void _PhysicsProcess(double delta)
    {
        if (_player.IsQueuedForDeletion() || GetTree().Paused
            || (GodotObject.IsInstanceValid(GameManager.Instance)
                && GameManager.Instance.State != GameManager.GameState.Playing))
        {
            CancelBuild();
            return;
        }

        if (Input.IsActionJustPressed("cancel_build"))
        {
            CancelBuild();
            return;
        }

        if (Input.IsActionJustPressed("select_tower_1")) SelectTower(0);
        else if (Input.IsActionJustPressed("select_tower_2")) SelectTower(1);
        else if (Input.IsActionJustPressed("select_tower_3")) SelectTower(2);

        if (Input.IsActionJustPressed("build"))
        {
            if (!_isBuilding)
            {
                _isBuilding = CreatePreview();
                UpdatePreview();
                return;
            }

            UpdatePreview();
            if (CanPlaceTower()) PlaceTower();
            return;
        }

        UpdatePreview();
    }

    private void SelectTower(int index)
    {
        if (_selectedTowerIndex == index) return;

        _selectedTowerIndex = index;
        if (_isBuilding) _isBuilding = CreatePreview();
    }

    // Для проверки используем настоящий коллайдер, не добавляя боевую башню в мир.
    private bool CreatePreview()
    {
        RemovePreview();
        int index = _selectedTowerIndex;
        if (index >= _towerScenes.Count || index >= _previewScenes.Count
            || _towerScenes[index] == null || _previewScenes[index] == null)
        {
            GD.PushWarning("BuildComponent: назначь сцены башен и превью в одинаковом порядке.");
            return false;
        }

        Node3D template = _towerScenes[index].Instantiate<Node3D>();
        CollisionShape3D collider = template.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        if (collider == null || collider.Shape == null)
        {
            template.Free();
            GD.PushError("BuildComponent: у башни нужен дочерний CollisionShape3D с формой.");
            return false;
        }

        _placementShape = collider.Shape;
        _shapeTransform = collider.Transform;
        template.Free();

        _preview = _previewScenes[index].Instantiate<Node3D>();
        GetTree().CurrentScene.AddChild(_preview);
        CollectPreviewMeshes(_preview);
        return true;
    }

    private void CollectPreviewMeshes(Node node)
    {
        if (node is MeshInstance3D mesh) _previewMeshes.Add(mesh);
        foreach (Node child in node.GetChildren()) CollectPreviewMeshes(child);
    }

    private void UpdatePreview()
    {
        if (!_isBuilding || !GodotObject.IsInstanceValid(_preview)) return;

        Vector3 forward = -_player.GlobalTransform.Basis.Z;
        forward.Y = 0f;
        Vector3 position = _player.GlobalPosition + forward.Normalized() * _buildDistance;
        position.Y = _groundHeight;
        _preview.GlobalTransform = new Transform3D(Basis.Identity, position);

        bool canPlace = CanPlaceTower();
        foreach (MeshInstance3D mesh in _previewMeshes)
        {
            // При допустимой позиции снова видны исходные материалы превью.
            mesh.MaterialOverride = canPlace ? null : _blockedMaterial;
        }
    }

    private bool CanPlaceTower()
    {
        if (!GodotObject.IsInstanceValid(_preview)
            || !GodotObject.IsInstanceValid(_floorMesh) || _placementShape == null)
        {
            return false;
        }

        // Пол в текущем проекте — визуальная плоскость без коллайдера.
        // Проверяем углы основания в локальных координатах меша с учётом его масштаба.
        Aabb bounds = _floorMesh.GetAabb();
        foreach (float x in new[] { -_edgeMargin, _edgeMargin })
        {
            foreach (float z in new[] { -_edgeMargin, _edgeMargin })
            {
                Vector3 corner = _floorMesh.ToLocal(_preview.GlobalPosition + new Vector3(x, 0f, z));
                if (corner.X < bounds.Position.X || corner.X > bounds.End.X
                    || corner.Z < bounds.Position.Z || corner.Z > bounds.End.Z)
                {
                    return false;
                }
            }
        }

        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = _placementShape,
            Transform = _preview.GlobalTransform * _shapeTransform,
            CollisionMask = _blockingMask,
            CollideWithBodies = true,
            CollideWithAreas = false
        };

        // Игрок тоже препятствие; большие Area3D обнаружения башен не учитываются.
        return _player.GetWorld3D().DirectSpaceState.IntersectShape(query, 1).Count == 0;
    }

    private void PlaceTower()
    {
        Transform3D placement = _preview.GlobalTransform;
        Node3D tower = _towerScenes[_selectedTowerIndex].Instantiate<Node3D>();
        GetTree().CurrentScene.AddChild(tower);
        tower.GlobalTransform = placement;
        CancelBuild();
    }

    private void CancelBuild()
    {
        _isBuilding = false;
        RemovePreview();
    }

    private void RemovePreview()
    {
        if (GodotObject.IsInstanceValid(_preview))
        {
            _preview.Hide();
            _preview.QueueFree();
        }

        _preview = null;
        _placementShape = null;
        _previewMeshes.Clear();
    }

    // Удаляем отдельное превью при паузе, смерти игрока и перезапуске сцены.
    public override void _Notification(int what)
    {
        if (what == NotificationPaused) CancelBuild();
    }

    public override void _ExitTree()
    {
        CancelBuild();
    }
}

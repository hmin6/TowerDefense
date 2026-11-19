using UnityEngine;

namespace MidnightHour
{
    /// <summary>
    /// Mouse placement: pick a tower (1-3 = party members' Personas, 4 = cycle Makoto's Personas,
    /// or click a card), a ghost follows the cursor
    /// (green = valid, red = blocked) with its range ring, left-click places, right-click / Esc cancels.
    /// With nothing selected, hover a tower and press X to dismiss it for a partial refund.
    /// </summary>
    public class TowerPlacer : MonoBehaviour
    {
        public static TowerPlacer Instance { get; private set; }

        [Tooltip("Snap placement to this grid size (meters). 0 = free placement.")]
        public float gridSize = 1f;
        [Tooltip("Minimum distance from the centre of the path.")]
        public float pathClearance = 1.7f;
        [Tooltip("Minimum distance between two towers.")]
        public float towerSpacing = 1.8f;
        [Tooltip("Half size of the buildable area (X, Z).")]
        public Vector2 mapHalfExtents = new Vector2(19.5f, 12.5f);

        public int Selected { get; private set; } = -1;
        public bool CurrentValid { get; private set; }
        public string InvalidReason { get; private set; }
        public bool GhostVisible { get { return ghost != null && ghost.activeSelf; } }
        public Tower HoveredTower { get; private set; }

        static readonly RaycastHit[] rayBuffer = new RaycastHit[16];
        static readonly Color ValidColor = new Color(0.25f, 1f, 0.45f);
        static readonly Color InvalidColor = new Color(1f, 0.2f, 0.2f);

        GameObject ghost;
        LineRenderer ring;
        int lastTint = -1; // -1 unknown, 0 invalid, 1 valid

        void Awake()
        {
            Instance = this;
            ring = VisualFactory.Line(transform, new Color(1f, 1f, 1f, 0.7f), 0.06f, "Range Ring");
            ring.enabled = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Select(int index)
        {
            var gm = GameManager.Instance;
            if (gm == null || index < 0 || index >= gm.Towers.Count || index == Selected)
            {
                Cancel();
                return;
            }
            Selected = index;
            DestroyGhost();
            ghost = Factory.BuildTowerGhost(gm.Towers[index]);
            ghost.SetActive(false);
            lastTint = -1;
        }

        /// <summary>Key 4: step through Makoto's Personas (the Wild Card), wrapping around.</summary>
        public void CycleWildCard()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            int first = gm.MemberTowerCount;
            int count = gm.Towers.Count - first;
            if (count <= 0) return;
            int next = Selected >= first ? first + (Selected - first + 1) % count : first;
            if (next == Selected) return; // only one Persona: keep it selected
            Select(next);
        }

        public void Cancel()
        {
            Selected = -1;
            DestroyGhost();
            if (ring != null) ring.enabled = false;
        }

        void DestroyGhost()
        {
            if (ghost != null) Destroy(ghost);
            ghost = null;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            HoveredTower = null;
            if (gm == null) return;

            if (!gm.CanPlaceNow)
            {
                if (Selected >= 0) Cancel();
                ring.enabled = false;
                return;
            }

            // Ignore keys on the exact frame the state changed (the same key may have closed a menu).
            if (Time.frameCount != gm.StateChangedFrame)
            {
                int members = gm.MemberTowerCount;
                if (InputCompat.Down(GameKey.Tower1) && members > 0) Select(0);
                if (InputCompat.Down(GameKey.Tower2) && members > 1) Select(1);
                if (InputCompat.Down(GameKey.Tower3) && members > 2) Select(2);
                if (InputCompat.Down(GameKey.Choice4)) CycleWildCard();
            }
            if (InputCompat.Down(GameKey.Cancel)) Cancel();

            Camera cam = Camera.main;
            if (cam == null) return;
            Vector2 mouse = InputCompat.MousePosition;
            bool overHud = HUD.Instance != null && HUD.Instance.IsPointerOverHUD(mouse);
            Ray ray = cam.ScreenPointToRay(mouse);

            if (Selected < 0)
            {
                ring.enabled = false;
                if (overHud) return;
                HoveredTower = RaycastTower(ray);
                if (HoveredTower != null)
                {
                    ring.enabled = true;
                    VisualFactory.SetCircle(ring, HoveredTower.transform.position, HoveredTower.Def.range);
                    if (InputCompat.Down(GameKey.Sell)) gm.SellTower(HoveredTower);
                }
                return;
            }

            if (InputCompat.RightClickDown)
            {
                Cancel();
                return;
            }

            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (overHud || !ground.Raycast(ray, out float enter))
            {
                ghost.SetActive(false);
                ring.enabled = false;
                CurrentValid = false;
                return;
            }

            Vector3 p = Snap(ray.GetPoint(enter));
            ghost.SetActive(true);
            ghost.transform.position = p;

            CurrentValid = Validate(p, out string reason);
            InvalidReason = reason;
            int tint = CurrentValid ? 1 : 0;
            if (tint != lastTint)
            {
                VisualFactory.Tint(ghost, CurrentValid ? ValidColor : InvalidColor, 0.9f);
                ring.startColor = ring.endColor = CurrentValid ? new Color(0.4f, 1f, 0.6f, 0.8f) : new Color(1f, 0.3f, 0.3f, 0.8f);
                lastTint = tint;
            }

            ring.enabled = true;
            VisualFactory.SetCircle(ring, p, gm.Towers[Selected].range);

            if (InputCompat.LeftClickDown && CurrentValid)
            {
                gm.TryBuildTower(Selected, p);
                lastTint = -1; // re-validate colours next frame (SP / limit changed)
            }
        }

        Vector3 Snap(Vector3 p)
        {
            p.y = 0f;
            if (gridSize > 0.01f)
            {
                p.x = Mathf.Round(p.x / gridSize) * gridSize;
                p.z = Mathf.Round(p.z / gridSize) * gridSize;
            }
            return p;
        }

        public bool Validate(Vector3 p, out string reason)
        {
            var gm = GameManager.Instance;
            var def = gm.Towers[Selected];

            if (Mathf.Abs(p.x) > mapHalfExtents.x || Mathf.Abs(p.z) > mapHalfExtents.y)
            {
                reason = "Outside the battlefield";
                return false;
            }
            if (EnemyPath.Instance != null && EnemyPath.Instance.DistanceToPathXZ(p) < pathClearance)
            {
                reason = "Too close to the Shadows' path";
                return false;
            }
            foreach (var t in Tower.All)
            {
                Vector3 d = t.transform.position - p;
                d.y = 0f;
                if (d.magnitude < towerSpacing)
                {
                    reason = "Too close to another tower";
                    return false;
                }
            }
            if (gm.TowerCount >= gm.TowerLimit)
            {
                reason = $"Tower limit reached ({gm.TowerLimit}). Press X on a tower to dismiss it.";
                return false;
            }
            if (gm.SP < def.cost)
            {
                reason = $"Not enough SP (need {def.cost}, have {gm.SP})";
                return false;
            }
            reason = null;
            return true;
        }

        static Tower RaycastTower(Ray ray)
        {
            int n = Physics.RaycastNonAlloc(ray, rayBuffer, 300f, ~0, QueryTriggerInteraction.Ignore);
            Tower best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var t = rayBuffer[i].collider.GetComponentInParent<Tower>();
                if (t != null && rayBuffer[i].distance < bestDist)
                {
                    best = t;
                    bestDist = rayBuffer[i].distance;
                }
            }
            return best;
        }
    }
}

using System;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;
using UnityEngine;

namespace PuzzleGame.Unity.Battle
{
    public sealed class BoardView : MonoBehaviour
    {
        private static readonly Color[] OrbColors =
        {
            new Color(.92f, .20f, .14f),
            new Color(.12f, .48f, .95f),
            new Color(.18f, .72f, .28f),
            new Color(.96f, .82f, .18f),
            new Color(.48f, .22f, .66f),
            new Color(.96f, .34f, .62f)
        };

        private static readonly string[] OrbLabels = { "F", "W", "N", "L", "D", "H" };

        private readonly SpriteRenderer[] renderers = new SpriteRenderer[BoardState.Columns * BoardState.Rows];
        private readonly TextMesh[] labels = new TextMesh[BoardState.Columns * BoardState.Rows];
        private readonly OrbType[] displayedOrbs = new OrbType[BoardState.Columns * BoardState.Rows];
        private readonly GameObject[] ownedCells = new GameObject[BoardState.Columns * BoardState.Rows];
        private Texture2D ownedTexture;
        private Sprite ownedSprite;
        private Material ownedMaterial;
        private bool initialized;

        public int CellCount { get { return initialized ? renderers.Length : 0; } }

        public void Initialize(BoardState board)
        {
            if (board == null) throw new ArgumentNullException("board");
            if (!initialized) ComposeCells();
            Refresh(board);
        }

        public void Refresh(BoardState board)
        {
            if (board == null) throw new ArgumentNullException("board");
            if (!initialized) throw new InvalidOperationException("BoardView must be initialized before refresh.");
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
            {
                var index = Index(x, y);
                var orb = board.Get(x, y);
                displayedOrbs[index] = orb;
                renderers[index].color = OrbColors[(int)orb];
                labels[index].text = OrbLabels[(int)orb];
            }
        }

        public OrbType GetDisplayedOrb(BoardPosition position)
        {
            return displayedOrbs[Index(position.X, position.Y)];
        }

        public string GetLabel(BoardPosition position)
        {
            return labels[Index(position.X, position.Y)].text;
        }

        public Color GetColor(BoardPosition position)
        {
            return renderers[Index(position.X, position.Y)].color;
        }

        private void ComposeCells()
        {
            ownedTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            ownedTexture.name = "Runtime Board Orb Texture";
            ownedTexture.hideFlags = HideFlags.DontSave;
            var center = 15.5f;
            var radiusSquared = 15f * 15f;
            for (var y = 0; y < ownedTexture.height; y++)
            for (var x = 0; x < ownedTexture.width; x++)
            {
                var dx = x - center;
                var dy = y - center;
                ownedTexture.SetPixel(x, y, dx * dx + dy * dy <= radiusSquared ? Color.white : Color.clear);
            }
            ownedTexture.Apply(false, true);

            ownedSprite = Sprite.Create(ownedTexture, new Rect(0f, 0f, 32f, 32f), new Vector2(.5f, .5f), 32f);
            ownedSprite.name = "Runtime Board Orb Sprite";
            ownedSprite.hideFlags = HideFlags.DontSave;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Sprites/Default shader is unavailable.");
            ownedMaterial = new Material(shader);
            ownedMaterial.name = "Runtime Board Orb Material";
            ownedMaterial.hideFlags = HideFlags.DontSave;

            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
            {
                var index = Index(x, y);
                var cell = new GameObject("Cell " + x + "," + y);
                ownedCells[index] = cell;
                cell.transform.SetParent(transform, false);
                cell.transform.localPosition = new Vector3(x - (BoardState.Columns - 1) * .5f, y - (BoardState.Rows - 1) * .5f, 0f);
                var renderer = cell.AddComponent<SpriteRenderer>();
                renderer.sprite = ownedSprite;
                renderer.sharedMaterial = ownedMaterial;
                renderers[index] = renderer;

                var labelObject = new GameObject("Label");
                labelObject.transform.SetParent(cell.transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0f, -.1f);
                var label = labelObject.AddComponent<TextMesh>();
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.characterSize = .12f;
                label.fontSize = 32;
                label.color = Color.white;
                labels[index] = label;
            }

            initialized = true;
        }

        private void OnDestroy()
        {
            for (var index = 0; index < ownedCells.Length; index++) DestroyOwned(ownedCells[index]);
            DestroyOwned(ownedMaterial);
            DestroyOwned(ownedSprite);
            DestroyOwned(ownedTexture);
            initialized = false;
        }

        private static int Index(int x, int y)
        {
            if (x < 0 || x >= BoardState.Columns || y < 0 || y >= BoardState.Rows)
                throw new ArgumentOutOfRangeException("position", "Board position is outside the board.");
            return y * BoardState.Columns + x;
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}

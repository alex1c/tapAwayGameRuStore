using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Visual proxy for one puzzle block. Selection maps to Core BlockId only;
	/// colliders never decide move legality.
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public sealed class BlockView : MonoBehaviour
	{
		[SerializeField] private Transform _arrowRoot;
		[SerializeField] private Renderer _bodyRenderer;

		private BlockId _blockId;
		private EscapeDirection _direction;
		private Color _baseColor;
		private MaterialPropertyBlock _propertyBlock;
		private static readonly int ColorId = Shader.PropertyToID("_Color");
		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		public BlockId BlockId => _blockId;
		public EscapeDirection EscapeDirection => _direction;
		public bool IsAnimating { get; private set; }

		/// <summary>
		/// Assigns visual parts created by the presenter (no Inspector wiring required).
		/// </summary>
		public void SetVisualParts(Transform arrowRoot, Renderer bodyRenderer)
		{
			_arrowRoot = arrowRoot;
			_bodyRenderer = bodyRenderer;
		}

		/// <summary>
		/// Binds this view to a Core block definition and orients the arrow.
		/// </summary>
		public void Bind(PuzzleBlock block, Color color)
		{
			_blockId = block.Id;
			_direction = block.EscapeDirection;
			_baseColor = color;
			name = "Block_" + block.Id.Value;
			transform.localPosition = GridToWorld(block.Position);
			OrientArrow(block.EscapeDirection);
			ApplyColor(color);
			gameObject.SetActive(true);
			IsAnimating = false;
		}

		public void SetAnimating(bool value)
		{
			IsAnimating = value;
		}

		/// <summary>
		/// Immediate selection acknowledgement before move feedback.
		/// </summary>
		public void PlaySelectFlash()
		{
			ApplyColor(Color.Lerp(_baseColor, Color.white, 0.55f));
		}

		public void ApplyPulseColor(Color color)
		{
			ApplyColor(color);
		}

		public void RestoreBaseColor()
		{
			ApplyColor(_baseColor);
		}

		public void SetHighlighted(bool highlighted)
		{
			if (highlighted)
			{
				ApplyColor(Color.Lerp(_baseColor, new Color(1f, 0.95f, 0.4f), 0.45f));
				if (_arrowRoot != null)
				{
					_arrowRoot.localScale = Vector3.one * 1.15f;
				}
			}
			else
			{
				RestoreBaseColor();
				if (_arrowRoot != null)
				{
					_arrowRoot.localScale = Vector3.one;
				}
			}
		}

		public void Hide()
		{
			IsAnimating = false;
			gameObject.SetActive(false);
		}

		/// <summary>Converts integer grid cells to local presentation space.</summary>
		public static Vector3 GridToWorld(GridPosition position)
		{
			return new Vector3(position.X, position.Y, position.Z);
		}

		/// <summary>Unit world vector for an escape direction.</summary>
		public static Vector3 DirectionToWorld(EscapeDirection direction)
		{
			EscapeDirectionUtil.GetStep(direction, out var dx, out var dy, out var dz);
			return new Vector3(dx, dy, dz);
		}

		private void OrientArrow(EscapeDirection direction)
		{
			if (_arrowRoot == null)
			{
				return;
			}

			var worldDir = DirectionToWorld(direction);
			if (worldDir.sqrMagnitude < 0.001f)
			{
				return;
			}

			_arrowRoot.localRotation = Quaternion.LookRotation(worldDir, Vector3.up);
		}

		private void ApplyColor(Color color)
		{
			if (_bodyRenderer == null)
			{
				return;
			}

			if (_propertyBlock == null)
			{
				_propertyBlock = new MaterialPropertyBlock();
			}

			_bodyRenderer.GetPropertyBlock(_propertyBlock);
			_propertyBlock.SetColor(ColorId, color);
			_propertyBlock.SetColor(BaseColorId, color);
			_bodyRenderer.SetPropertyBlock(_propertyBlock);
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (_bodyRenderer == null)
			{
				_bodyRenderer = GetComponentInChildren<Renderer>();
			}
		}
#endif
	}
}

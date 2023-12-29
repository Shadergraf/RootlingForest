namespace UnityEngine.UI
{
	public class FlexibleGridLayout : LayoutGroup
	{
		public enum Alignment
		{
			Horizontal,
			Vertical
		}
		public enum FitType
		{
			Uniform,
			Width,
			Height,
			FixedRows,
			FixedColumns,
			FixedBoth
		}

		public Alignment alignment;
		[Space]
		public FitType fitType;
		[Min(1)]
		public int columns;
		[Min(1)]
		public int rows;
		[Space]
		[Min(0)]
		public Vector2 spacing;
		public Vector2 cellSize;

		public bool fitX;
		public bool fitY;

		public bool NudgeLastItemsOver;

		public override void CalculateLayoutInputVertical()
		{
			base.CalculateLayoutInputHorizontal();
			float sqrRt;

			// TODO slow
			int layoutChildCount = 0;
			for (int i = 0; i < transform.childCount; i++)
			{
				var c = transform.GetChild(i);
				if (c.TryGetComponent(out LayoutElement le) && le.ignoreLayout)
					continue;
				layoutChildCount++;
			}

			switch (fitType)
			{
				case FitType.Uniform:
				default:
					fitX = fitY = true;
					sqrRt = Mathf.Sqrt(layoutChildCount);
					rows = Mathf.CeilToInt(sqrRt);
					columns = Mathf.CeilToInt(sqrRt);
					rows = Mathf.CeilToInt(layoutChildCount / (float)columns);
					columns = Mathf.CeilToInt(layoutChildCount / (float)rows);
					break;
				case FitType.Width:
					fitX = fitY = true;
					sqrRt = Mathf.Sqrt(layoutChildCount);
					rows = Mathf.CeilToInt(sqrRt);
					columns = Mathf.CeilToInt(sqrRt);
					rows = Mathf.CeilToInt(layoutChildCount / (float)columns);
					break;
				case FitType.Height:
					fitX = fitY = true;
					sqrRt = Mathf.Sqrt(layoutChildCount);
					rows = Mathf.CeilToInt(sqrRt);
					columns = Mathf.CeilToInt(sqrRt);
					columns = Mathf.CeilToInt(layoutChildCount / (float)rows);
					break;
				case FitType.FixedRows:
					fitX = fitY = false;
					columns = Mathf.CeilToInt(layoutChildCount / (float)rows);
					break;
				case FitType.FixedColumns:
					fitX = fitY = false;
					rows = Mathf.CeilToInt(layoutChildCount / (float)columns);
					break;
				case FitType.FixedBoth:
					fitX = fitY = false;
					break;
			}

			float cellWidth;
			float cellHeight;

			switch (alignment)
			{
				case Alignment.Horizontal:
					cellWidth = (this.rectTransform.rect.width / (float)columns) - ((spacing.x / (float)columns) * (columns - 1)) - (padding.left / (float)columns) - (padding.right / (float)columns);
					cellHeight = (this.rectTransform.rect.height / (float)rows) - ((spacing.y / (float)rows) * (rows - 1)) - (padding.top / (float)rows) - (padding.bottom / (float)rows);
					break;
				case Alignment.Vertical:
				default:
					cellHeight = (this.rectTransform.rect.width / (float)columns) - ((spacing.x / (float)columns) * (columns - 1)) - (padding.left / (float)columns) - (padding.right / (float)columns);
					cellWidth = (this.rectTransform.rect.height / (float)rows) - ((spacing.y / (float)rows) * (rows - 1)) - (padding.top / (float)rows) - (padding.bottom / (float)rows);
					break;
			}

			cellSize.x = fitX ? (cellWidth <= 0 ? cellSize.x : cellWidth) : cellSize.x;
			cellSize.y = fitY ? (cellHeight <= 0 ? cellSize.y : cellHeight) : cellSize.y;

			int columnCount = 0;
			int rowCount = 0;

			for (int i = 0; i < rectChildren.Count; i++)
			{
				var item = rectChildren[i];
				float xPos;
				float yPos;
				float xLastItemOffset = 0;

				switch (alignment)
				{
					case Alignment.Horizontal:
						rowCount = i / columns;
						columnCount = i % columns;
						if (NudgeLastItemsOver && rowCount == (rectChildren.Count / columns)) { xLastItemOffset = (cellSize.x + padding.left) / 2; }
						break;
					case Alignment.Vertical:
					default:
						rowCount = i / rows;
						columnCount = i % rows;
						if (NudgeLastItemsOver && rowCount == (rectChildren.Count / rows)) { xLastItemOffset = (cellSize.x + padding.left) / 2; }
						break;
				}

				xPos = (cellSize.x * columnCount) + (spacing.x * columnCount) + padding.left + xLastItemOffset;
				yPos = (cellSize.y * rowCount) + (spacing.y * rowCount) + padding.top;

				switch (m_ChildAlignment)
				{
					case TextAnchor.UpperLeft:
					default:
						//No need to change xPos;
						//No need to change yPos;
						break;
					case TextAnchor.UpperCenter:
						xPos += (0.5f * (this.gameObject.GetComponent<RectTransform>().sizeDelta.x + (spacing.x + padding.left + padding.left) - (columns * (cellSize.x + spacing.x + padding.left)))); //Center xPos
																																																		//No need to change yPos;
						break;
					case TextAnchor.UpperRight:
						xPos = -xPos + this.gameObject.GetComponent<RectTransform>().sizeDelta.x - cellSize.x; //Flip xPos to go bottom-up
																											   //No need to change yPos;
						break;
					case TextAnchor.MiddleLeft:
						//No need to change xPos;
						yPos += (0.5f * (this.gameObject.GetComponent<RectTransform>().sizeDelta.y + (spacing.y + padding.top + padding.top) - (rows * (cellSize.y + spacing.y + padding.top)))); //Center yPos
						break;
					case TextAnchor.MiddleCenter:
						xPos += (0.5f * (this.gameObject.GetComponent<RectTransform>().sizeDelta.x + (spacing.x + padding.left + padding.left) - (columns * (cellSize.x + spacing.x + padding.left)))); //Center xPos
						yPos += (0.5f * (this.gameObject.GetComponent<RectTransform>().sizeDelta.y + (spacing.y + padding.top + padding.top) - (rows * (cellSize.y + spacing.y + padding.top)))); //Center yPos
						break;
					case TextAnchor.MiddleRight:
						xPos = -xPos + this.gameObject.GetComponent<RectTransform>().sizeDelta.x - cellSize.x; //Flip xPos to go bottom-up
						yPos += (0.5f * (this.gameObject.GetComponent<RectTransform>().sizeDelta.y + (spacing.y + padding.top + padding.top) - (rows * (cellSize.y + spacing.y + padding.top)))); //Center yPos
						break;
					case TextAnchor.LowerLeft:
						//No need to change xPos;
						yPos = -yPos + this.gameObject.GetComponent<RectTransform>().sizeDelta.y - cellSize.y; //Flip yPos to go Right to Left
						break;
					case TextAnchor.LowerCenter:
						xPos += (0.5f * (this.gameObject.GetComponent<RectTransform>().sizeDelta.x + (spacing.x + padding.left + padding.left) - (columns * (cellSize.x + spacing.x + padding.left)))); //Center xPos
						yPos = -yPos + this.gameObject.GetComponent<RectTransform>().sizeDelta.y - cellSize.y; //Flip yPos to go Right to Left
						break;
					case TextAnchor.LowerRight:
						xPos = -xPos + this.gameObject.GetComponent<RectTransform>().sizeDelta.x - cellSize.x; //Flip xPos to go bottom-up
						yPos = -yPos + this.gameObject.GetComponent<RectTransform>().sizeDelta.y - cellSize.y; //Flip yPos to go Right to Left
						break;
				}

				SetChildAlongAxis(item, 0, xPos, cellSize.x);
				SetChildAlongAxis(item, 1, yPos, cellSize.y);
			}

		}

		public override void SetLayoutHorizontal()
		{

		}

		public override void SetLayoutVertical()
		{

		}

		// TODO take FitType into account
        public override float preferredWidth => m_Padding.left + m_Padding.right + cellSize.x * columns + spacing.x * Mathf.Min(columns - 1);
        public override float preferredHeight => m_Padding.top + m_Padding.bottom + cellSize.y * rows + spacing.y * Mathf.Min(rows - 1);
    }
}
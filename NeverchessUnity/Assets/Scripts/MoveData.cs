
public struct MoveData
{
	public PieceCode upgrade;
	public bool shortCastle;
	public bool longCastle;
	public bool doublePawnMove;
	public bool enPassant;
	public int xStart;
	public int yStart;
	public int xEnd;
	public int yEnd;

	public MoveData(int xStart, int yStart, int xEnd, int yEnd)
	{
		upgrade = PieceCode.EMPTY;
		shortCastle = false;
		longCastle = false;
		doublePawnMove = false;
		enPassant = false;
		this.xStart = xStart;
		this.yStart = yStart;
		this.xEnd = xEnd;
		this.yEnd = yEnd;
	}
}

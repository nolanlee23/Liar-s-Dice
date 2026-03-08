using Unity.Netcode;

public class Player
{
    public int playerNumber;
    public int diceCount;
    public int[] dice;

    public Player(int playerNumber)
    {
        this.playerNumber = playerNumber;
        diceCount = 5;
        dice = new int[diceCount];
    }
}
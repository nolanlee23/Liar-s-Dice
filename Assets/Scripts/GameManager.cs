using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public Player player1, player2;
    public Player currentPlayer;
    public UIManager uiManager;

    private int currentBidQuantity;
    private int currentBidFace;
    private bool actionable;
    
    void Start()
    {
        player1 = new Player(1);
        player2 = new Player(2);

        currentPlayer = player2;
        
        StartRound();
    }

    void StartRound()
    {
        currentBidQuantity = 0;
        currentBidFace = 0;

        RollDice(player1);
        RollDice(player2);
        SwitchTurns();

        uiManager.SetGameStateColor(Color.white);
        uiManager.UpdateDice(player1, player2);
        uiManager.HideChallengeButton();
        uiManager.UpdateTurnIndicators(currentPlayer, player1, player2);
        uiManager.SetLossIndicator(player1, false);
        uiManager.SetLossIndicator(player2, false);


        actionable = true;
        
    }

    void RollDice(Player p)
    {
        for (int i = 0; i < p.dice.Length; i++)
        {
            if (i < p.diceCount)
                p.dice[i] = Random.Range(1, 7);
            else
                p.dice[i] = 0;
        }
    }

    Player GetOtherPlayer(Player p)
    {
        return (p == player1) ? player2 : player1;
    }

    public void PlaceBid(int quantity, int face)
    {
        if (!actionable) return;

        // Check for bid validity
        if (quantity <= currentBidQuantity && face <= currentBidFace)
        {
            uiManager.UpdateGameState("Invalid Bid; must be higher face value or quantity");
            return;
        }

        currentBidQuantity = quantity;
        currentBidFace = face;
        uiManager.UpdateBid(quantity, face);

        SwitchTurns();
    }

    public void Challenge()
    {
        if (!actionable) return;

        int totalFaceCount = CountTotalFaces(currentBidFace);
        if (totalFaceCount >= currentBidQuantity)
        {
            // Challenge failed; current player loses round
            RoundLost(currentPlayer);

        }
        else
        {
            // Challenge succeeded; opposite player loses round
            RoundLost(GetOtherPlayer(currentPlayer));
        }
    }

    // Count total number of times a given face appears in both players' hands
    int CountTotalFaces(int face)
    {
        int count = 0;
        foreach (int d in player1.dice) if (d == face) count++;
        foreach (int d in player2.dice) if (d == face) count++;
        return count;
    }

    void RoundLost(Player p)
    {
        p.diceCount--;
        actionable = false;

        if (p.diceCount <= 0)
        {
            uiManager.UpdateGameState("Player " + GetOtherPlayer(p).playerNumber + " wins!");
            uiManager.UpdateDice(player1, player2);
            uiManager.HideChallengeButton();
            
        }
        else
        {
            uiManager.SetGameStateColor(Color.red);
            uiManager.SetLossIndicator(p, true);
            StartCoroutine(WaitThen(4f, StartRound));

        }
    }

    IEnumerator WaitThen(float seconds, System.Action callback)
    {
        yield return new WaitForSeconds(seconds);
        callback();
    }

    void SwitchTurns()
    {
        currentPlayer = GetOtherPlayer(currentPlayer);
        uiManager.UpdateTurnIndicators(currentPlayer, player1, player2);
    }
}

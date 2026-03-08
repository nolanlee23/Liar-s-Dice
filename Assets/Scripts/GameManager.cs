using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public Player player1, player2;
    public UIManager uiManager;
    private bool actionable;

    public NetworkVariable<int> currentBidQuantity = new NetworkVariable<int>(0);
    public NetworkVariable<int> currentBidFace = new NetworkVariable<int>(0);

    public NetworkVariable<int> currentPlayerTurn = new NetworkVariable<int>(1);
    Player currentPlayer => currentPlayerTurn.Value == 0 ? player1 : player2;

    public override void OnNetworkSpawn()
    {
        player1 = new Player(0);
        player2 = new Player(1);

        if (IsServer)
        {
            StartGame();
            uiManager.SetLocalPlayer(player1);
        }
        else
        {
            RequestServerRpc();
            uiManager.SetLocalPlayer(player2);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestServerRpc()
    {
        SyncRoundClientRpc(player1.dice, player2.dice, player1.diceCount, player2.diceCount);
    }

    void StartGame()
    {
        currentPlayerTurn.Value = Random.Range(0, 2);
        
        StartRound();
    }

    void StartRound()
    {
        if (!IsServer) return;

        currentBidQuantity.Value = 0;
        currentBidFace.Value = 0;

        RollDice(player1);
        RollDice(player2);

        actionable = true;

        SyncRoundClientRpc(player1.dice, player2.dice, player1.diceCount, player2.diceCount);
        
    }

    [ClientRpc]
    void SyncRoundClientRpc(int[] p1Dice, int[] p2Dice, int p1DiceCount, int p2DiceCount) 
    {
        player1.dice = p1Dice;
        player2.dice = p2Dice;
        player1.diceCount = p1DiceCount;
        player2.diceCount = p2DiceCount;
        UpdateDiceUI();
        uiManager.SetGameStateColor(Color.white);
        uiManager.HideChallengeButton();
        uiManager.UpdateTurnIndicators(currentPlayer);
        uiManager.SetLossIndicator(player1, false);
        uiManager.SetLossIndicator(player2, false);
    }

    void UpdateDiceUI() 
    {
        Player local = IsServer ? player1 : player2;
        Player remote = IsServer ? player2 : player1;
        uiManager.UpdateDice(local, remote);
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlaceBidServerRpc(int quantity, int face)
    {
        if (!actionable) return;

        // Check for bid validity
        if (quantity <= currentBidQuantity.Value && face <= currentBidFace.Value) return;

        currentBidQuantity.Value = quantity;
        currentBidFace.Value = face;

        SwitchTurns();
        UpdateBidClientRpc(quantity, face, currentPlayerTurn.Value);
    }

    [ClientRpc]
    void UpdateBidClientRpc(int quantity, int face, int nextPlayerTurn) 
    {
        uiManager.UpdateBid(quantity, face);
        uiManager.UpdateTurnIndicators(nextPlayerTurn == 0 ? player1 : player2);
        uiManager.ShowChallengeButton();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ChallengeServerRpc()
    {
        if (!actionable) return;

        int totalFaceCount = CountTotalFaces(currentBidFace.Value);
        if (totalFaceCount >= currentBidQuantity.Value)
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
            GameOverClientRpc(GetOtherPlayer(p).playerNumber);
        }
        else
        {
            RoundLostClientRpc(p.playerNumber);
            StartCoroutine(WaitThen(4f, StartRound));
        }
    }

    [ClientRpc]
    void GameOverClientRpc(int winnerNumber) 
    {
        uiManager.UpdateGameState("Player " + winnerNumber + " wins!");
        UpdateDiceUI();
        uiManager.HideChallengeButton();
    }

    [ClientRpc]
    void RoundLostClientRpc(int losingPlayerNumber) 
    {
        Player loser = losingPlayerNumber == 0 ? player1 : player2;
        uiManager.SetGameStateColor(Color.red);
        uiManager.SetLossIndicator(loser, true);
    }

    IEnumerator WaitThen(float seconds, System.Action callback)
    {
        yield return new WaitForSeconds(seconds);
        callback();
    }

    void SwitchTurns()
    {
        currentPlayerTurn.Value = GetOtherPlayer(currentPlayer).playerNumber;
    }
}

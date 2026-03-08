using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public Player player1, player2;
    public UIManager uiManager;
    private bool actionable;

    Player currentPlayer => currentPlayerTurn.Value == 0 ? player1 : player2;

    public NetworkVariable<int> currentBidQuantity = new NetworkVariable<int>(0);
    public NetworkVariable<int> currentBidFace = new NetworkVariable<int>(0);
    public NetworkVariable<int> currentPlayerTurn = new NetworkVariable<int>(1);

    // Initialize states for both host and remote client, determine local and remote player
    public override void OnNetworkSpawn()
    {
        player1 = new Player(0);
        player2 = new Player(1);

        if (IsServer)
        {
            uiManager.SetLocalPlayer(player1);
            StartGame();
        }
        else
        {
            uiManager.SetLocalPlayer(player2);
            RequestServerRpc();
        }
    }

    // Accounts for the difference in connection time delay and state variables properly updating
    //      by sending the request to the server instead of the client
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

    // Syncs local variables with the server's
    [ClientRpc]
    void SyncRoundClientRpc(int[] p1Dice, int[] p2Dice, int p1DiceCount, int p2DiceCount) 
    {
        player1.dice = p1Dice;
        player2.dice = p2Dice;
        player1.diceCount = p1DiceCount;
        player2.diceCount = p2DiceCount;
        UpdateDiceUI();
        uiManager.SetGameStateColor(Color.white);
        uiManager.UpdateTurnIndicators(currentPlayer);
        uiManager.SetLossIndicator(player1, false);
        uiManager.SetLossIndicator(player2, false);
        uiManager.HideRemoteDice();
    }

    // Determine which player is the local vs remote based on if hosting or not
    void UpdateDiceUI() 
    {
        Player local = IsServer ? player1 : player2;
        Player remote = IsServer ? player2 : player1;
        uiManager.UpdateDice(local, remote);
    }

    // Rolls [remaining dice] times, sets local variable
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

    // Server-side function called when Place Bid is pressed, updates network variables
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

    // Updates all client's bid and turn indicators
    [ClientRpc]
    void UpdateBidClientRpc(int quantity, int face, int nextPlayerTurn) 
    {
        uiManager.UpdateBid(quantity, face);
        uiManager.UpdateTurnIndicators(nextPlayerTurn == 0 ? player1 : player2);
    }

    // Server-side function called when Liar! is pressed, determines outcome of round
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

    // Decide whether game is over when out of dice or roll next round if not
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

    // Ends the game client-side and displays final game state
    [ClientRpc]
    void GameOverClientRpc(int winnerNumber) 
    {
        uiManager.UpdateGameState("Player " + (winnerNumber + 1) + " wins!");
        actionable = false;
        UpdateDiceUI();
        uiManager.RevealRemoteDice();
    }

    // Displays end-of-round state client-side
    [ClientRpc]
    void RoundLostClientRpc(int losingPlayerNumber) 
    {
        Player loser = losingPlayerNumber == 0 ? player1 : player2;
        uiManager.SetGameStateColor(Color.red);
        uiManager.SetLossIndicator(loser, true);
        uiManager.RevealRemoteDice();
    }

    // Allows for a delay to see the results of a challenge
    IEnumerator WaitThen(float seconds, System.Action callback)
    {
        yield return new WaitForSeconds(seconds);
        callback();
    }

    void SwitchTurns()
    {
        currentPlayerTurn.Value = GetOtherPlayer(currentPlayer).playerNumber;
    }

    Player GetOtherPlayer(Player p)
    {
        return (p == player1) ? player2 : player1;
    }
}

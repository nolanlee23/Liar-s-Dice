using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GameManager gameManager;

    private Player localPlayer;

    public TextMeshProUGUI currentBidText;
    public TextMeshProUGUI gameStateText;
    public TextMeshProUGUI displayX;
    public TextMeshProUGUI displayS;

    public TMP_InputField quantityInput;
    public TMP_InputField faceInput;

    public Button challengeButton;
    public Button placeBidButton;
    public Button hostButton;
    public Button joinButton;

    public Sprite blackDie;
    public Sprite[] dieFaces;
    public Image[] localDiceImages;
    public Image[] remoteDiceImages;
    public Image localTurnIndicator;
    public Image remoteTurnIndicator;
    public Image localLossIndicator;
    public Image remoteLossIndicator;

    public void SetLocalPlayer(Player p) 
    {
        localPlayer = p;
    }

    public void UpdateDice(Player local, Player remote) 
    {
        // Display remaining dice as images, set lost dice to Color.clear
        for (int i = 0; i < 5; i++) 
        {
            // Display player 1 dice
            if (local.dice[i] == 0) 
                localDiceImages[i].color = Color.clear;
            else 
                localDiceImages[i].sprite = dieFaces[local.dice[i] - 1];

            // Display player 2 dice
            if (remote.dice[i] == 0) 
                remoteDiceImages[i].color = Color.clear;
            else 
                remoteDiceImages[i].sprite = dieFaces[remote.dice[i] - 1];
        }
    }

    public void UpdateBid(int quantity, int face)
    {
        currentBidText.text = quantity + " x " + face + "'s";
        challengeButton.gameObject.SetActive(true);
    }

    public void UpdateGameState(string state)
    {
        gameStateText.text = state;
    }

    public void UpdateTurnIndicators(Player currentPlayer) 
    {
        if (localPlayer == null) return;
        bool isLocalTurn = currentPlayer == localPlayer;

        localTurnIndicator.color = isLocalTurn ? Color.white : Color.clear;
        remoteTurnIndicator.color = isLocalTurn ? Color.clear : Color.white;

        // Deactivate interactable UI elements when it is not the client's turn
        quantityInput.gameObject.SetActive(isLocalTurn);
        faceInput.gameObject.SetActive(isLocalTurn);
        challengeButton.gameObject.SetActive(isLocalTurn);
        placeBidButton.gameObject.SetActive(isLocalTurn);
        displayX.gameObject.SetActive(isLocalTurn);
        displayS.gameObject.SetActive(isLocalTurn);
    }

    public void SetGameStateColor(Color color) 
    {
        gameStateText.color = color;
    }

    public void OnBidPressed()
    {
        EventSystem.current.SetSelectedGameObject(null);

        int quantity = int.Parse(quantityInput.text);
        int face = int.Parse(faceInput.text);
        gameManager.PlaceBidServerRpc(quantity, face);

        quantityInput.text = "";
        faceInput.text = "";
    }

    public void OnChallengePressed()
    {
        EventSystem.current.SetSelectedGameObject(null);
        gameManager.ChallengeServerRpc();
    }

    public void OnHostPressed() 
    {
        Debug.Log("Host pressed");

        // Manually force allowing external connections
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        transport.SetConnectionData("0.0.0.0", 7777, "0.0.0.0");
        NetworkManager.Singleton.StartHost();
    }

    public void OnJoinPressed() 
    {
        Debug.Log("Join pressed");
        NetworkManager.Singleton.StartClient();
    }

    public void SetLossIndicator(Player loser, bool show) 
    {
        Image indicator = loser == localPlayer ? localLossIndicator : remoteLossIndicator;
        indicator.color = show ? Color.white : Color.clear;
    }

    public void RevealRemoteDice() 
    {
        Player remote = gameManager.player1 == localPlayer ? gameManager.player2 : gameManager.player1;
        
        for (int i = 0; i < 5; i++) 
        {
            if (remote.dice[i] != 0)
                remoteDiceImages[i].sprite = dieFaces[remote.dice[i] - 1];
        }
    }

    public void HideRemoteDice() 
    {
        Player remote = gameManager.player1 == localPlayer ? gameManager.player2 : gameManager.player1;

        for (int i = 0; i < 5; i++) 
        {
            if (remote.dice[i] != 0)
                remoteDiceImages[i].sprite = blackDie;
        }
    }
}

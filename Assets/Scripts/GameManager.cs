using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using static GameManager;

public class GameManager : NetworkBehaviour
{
    
    public static GameManager Instance {  get; private set; }

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public class OnClickedOnGridPositionEventArgs: EventArgs
    {
        public int x;
        public int y;
        public PlayerType playerType;
    }
    public event EventHandler OnGameStarted;
    public event EventHandler OnCurrentPlayablePlayerTypeChanged;
    public event EventHandler<OnGameWinEventArgs> onGameWin;
    public event EventHandler OnRematch;
    public event EventHandler OnGameTied;
    public event EventHandler OnScoreChanged;
    public event EventHandler OnPlaceObject;
    public class OnGameWinEventArgs : EventArgs
    {
        public Line line;
        public PlayerType winPlayerType;
    }
    public enum PlayerType
    {
        None,
        Cross,
        Circle,
    }
    public enum Orientation
    {
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB,
    }
    public struct Line
    {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPostion;
        public Orientation orientation;
    }


    private PlayerType localPlayerType;
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    private PlayerType[,] playerTypeArray;
    private List<Line> lineList;
    private NetworkVariable<int> playerCrossScore = new NetworkVariable<int>();
    private NetworkVariable<int> playerCircleScore = new NetworkVariable<int>();
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GameManager Instance");
        }
        Instance = this;

        playerTypeArray = new PlayerType[3, 3];

        lineList = new List<Line>
        {
            //HORIZONTAL LINES
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0)},
                centerGridPostion = new Vector2Int(1,0),
                orientation = Orientation.Horizontal
            },
             new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1)},
                centerGridPostion = new Vector2Int(1,1),
                orientation = Orientation.Horizontal
            },
              new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2)},
                centerGridPostion = new Vector2Int(1,2),
                orientation = Orientation.Horizontal
            },

            //VERTICAL LINES

             new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2)},
                centerGridPostion = new Vector2Int(0,1),
                orientation = Orientation.Vertical
            },
              new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2)},
                centerGridPostion = new Vector2Int(1,1),
                orientation = Orientation.Vertical
            },
               new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(2,0), new Vector2Int(2,1), new Vector2Int(2,2)},
                centerGridPostion = new Vector2Int(2,1),
                orientation = Orientation.Vertical
            },

            //DIAGONAL LINES

             new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,0), new Vector2Int(1,1), new Vector2Int(2,2)},
                centerGridPostion = new Vector2Int(1,1),
                orientation = Orientation.DiagonalA
            },
              new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,2), new Vector2Int(1,1), new Vector2Int(2,0)},
                centerGridPostion = new Vector2Int(1,1),
                orientation = Orientation.DiagonalB
            }
        };
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log(NetworkManager.Singleton.LocalClientId);

        if(NetworkManager.Singleton.LocalClientId == 0)
        {
            localPlayerType = PlayerType.Cross;
        }
        else
        {
            localPlayerType = PlayerType.Circle;
        }


        if (IsServer)
        {
            
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        currentPlayablePlayerType.OnValueChanged += (PlayerType oldPlayerType, PlayerType newPlayerType) =>
        {
            OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
        };

        playerCrossScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };
        playerCircleScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };
    }
    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        if(NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            //Start game
            currentPlayablePlayerType.Value = PlayerType.Cross;
            TriggerOnGameStartedRpc();
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc()
    {
        OnGameStarted?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType)
    {
        Debug.Log("ClickedOnGridPosition" +  x + ", " + y);
        if(playerType != currentPlayablePlayerType.Value)
        {
            return;
        }
        if (playerTypeArray[x,y] != PlayerType.None){
            return;
        }

        playerTypeArray[x,y] = playerType;
        TriggerOnPlaceObjectRpc();
        

        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs
        {
            x = x,
            y = y,
            playerType = playerType,
        });

        switch(currentPlayablePlayerType.Value)
        {
            default:
                case PlayerType.Cross:
                    currentPlayablePlayerType.Value = PlayerType.Circle;
                    break;
                case PlayerType.Circle:
                    currentPlayablePlayerType.Value = PlayerType.Cross;
                    break;
        }

        TestWinner();
        //TriggerOnCurrentPlayablePlayerTypeChangedRpc();
    }
    //[Rpc(SendTo.ClientsAndHost)]
    //private void TriggerOnCurrentPlayablePlayerTypeChangedRpc()
    //{
    //    OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
    //}
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnPlaceObjectRpc()
    {
        OnPlaceObject?.Invoke(this, EventArgs.Empty);
    }
    private bool TestWinnerGridLine(Line line)
    {
        return TestWinnerLine(playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
                          playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
                          playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]);

    }
    private bool TestWinnerLine(PlayerType aPlayerType, PlayerType bPlayerType, PlayerType cPlayerType)
    {
        return
            aPlayerType != PlayerType.None &&
            aPlayerType == bPlayerType &&
            bPlayerType == cPlayerType;

    }
    private void TestWinner()
    {

        for(int i = 0; i<lineList.Count; i++)
        {
            Line line = lineList[i];
            if (TestWinnerGridLine(line))
            {
                Debug.Log("Winner");
                currentPlayablePlayerType.Value = PlayerType.None;
                PlayerType winPlayerType = playerTypeArray[line.centerGridPostion.x, line.centerGridPostion.y];
                switch (winPlayerType)
                {
                    case PlayerType.Cross:
                        playerCrossScore.Value++;
                        break;
                    case PlayerType.Circle:
                        playerCircleScore.Value++;
                        break;
                }
                TriggerOnGameWinRpc(i, winPlayerType);
                return;
            }
        }
        bool hasTie = true;
        for (int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(1); y++)
            {
                if (playerTypeArray[x,y] == PlayerType.None)
                {
                    hasTie = false;
                    break;
                }
            }

            if (hasTie) {
                TriggerOnGameTiedRpc();
            }
        }
        //if (TestWinnerLine(playerTypeArray[0,0], playerTypeArray[1,0], playerTypeArray[2,0]))
        //{
        //    Debug.Log("Winner");
        //    currentPlayablePlayerType.Value = PlayerType.None;
        //    onGameWin?.Invoke(this, new OnGameWinEventArgs
        //    {
        //        centerGridPosition = new Vector2Int(1, 0)
        //    });
        //}
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameTiedRpc()
    {
        OnGameTied?.Invoke(this, EventArgs.Empty);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameWinRpc(int lineIndex, PlayerType winPlayerType)
    {
        Line line = lineList[lineIndex];
        onGameWin?.Invoke(this, new OnGameWinEventArgs
        {
            line = line,
            winPlayerType = winPlayerType
        });
    }
    [Rpc(SendTo.Server)]
    public void RematchRpc()
    {
        for(int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(1); y++)
            {
                playerTypeArray[x,y] = PlayerType.None;
            }
        }
        currentPlayablePlayerType.Value = PlayerType.Cross;

        TriggerOnRematchRpc();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnRematchRpc()
    {
        OnRematch?.Invoke(this, EventArgs.Empty);
    }
    public PlayerType GetLocalPlayerType()
    {
        return localPlayerType;
    }

    public PlayerType GetCurrentPlayablePlayerType()
    {
        return currentPlayablePlayerType.Value;
    }

    public void GetScores(out int playerCrossScore, out int playerCircleScore)
    {
        playerCrossScore = this.playerCrossScore.Value;
        playerCircleScore = this.playerCircleScore.Value;
    }
}

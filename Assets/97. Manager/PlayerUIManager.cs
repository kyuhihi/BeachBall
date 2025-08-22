using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    public struct PlayerUI
    {
        public GameObject PlayerObject;
        public GameObject BottomUI;
    }

    [SerializeField] private GameObject playerBottomUIprefab;
    private List<PlayerUI> Players = new List<PlayerUI>();

    // Raycast �ɼ�
    [SerializeField] private LayerMask groundMask = ~0;   // ���? ���̾� �⺻
    private float rayStartHeight = 0.0f; // �÷��̾� ������ ���?
    private float maxRayDistance = 100f;  // �ִ� �Ÿ�
     private float hoverHeight = 0.1f;   // ���� ���� ��¦ ����

    void Start()
    {
        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            PlayerUI playerUI = new PlayerUI
            {
                PlayerObject = player,
                BottomUI = Instantiate(playerBottomUIprefab, player.transform)
            };
            Players.Add(playerUI);
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var playerUI in Players)
        {
            var p = playerUI.PlayerObject.transform.position;
            var origin = p + Vector3.up * rayStartHeight;
            Vector3 targetPos;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                targetPos = hit.point + Vector3.up * hoverHeight;
            }
            else
            {
                targetPos = p ;
            }
            playerUI.BottomUI.transform.position = targetPos;
            playerUI.BottomUI.transform.rotation = Quaternion.identity;
            playerUI.BottomUI.GetComponent<Renderer>().material.SetColor("_Color", playerUI.PlayerObject.GetComponent<IPlayerInfo>().m_PlayerDefaultColor);

        }
    }
}

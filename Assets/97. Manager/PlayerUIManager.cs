using System.Collections.Generic;
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

    // Raycast 옵션
    [SerializeField] private LayerMask groundMask = ~0;   // 모든 레이어 기본
    private float rayStartHeight = 0.0f; // 플레이어 위에서 쏘기
    private float maxRayDistance = 100f;  // 최대 거리
     private float hoverHeight = 0.1f;   // 지면 위로 살짝 띄우기
    private float smoothLerp = 0f;      // 부드럽게 따라가기(0이면 즉시)

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

            if (smoothLerp > 0f)
            {
                playerUI.BottomUI.transform.position =
                    Vector3.Lerp(playerUI.BottomUI.transform.position, targetPos, Time.deltaTime * smoothLerp);
            }
            else
            {
                playerUI.BottomUI.transform.position = targetPos;
            }
        }
    }
}

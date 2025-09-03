using UnityEngine;
using System.Collections.Generic;

public class CheerUpCube : MonoBehaviour
{
    bool bSetPlayer = false;
    private const string TopGameObjName = "BoxTop";
    private GameObject m_TopGameObj;
    void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.name == TopGameObjName)
            {
                m_TopGameObj = child.gameObject;
                break;
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (!bSetPlayer)
        {
            var players = GameObject.FindGameObjectsWithTag("Player");

            foreach (var player in players)
            {
                if (!player.GetComponent<AwardAnimSelector>().GetSelected())
                    continue;


                if (player.GetComponent<AwardAnimSelector>().GetWinLoseCheer() == AwardAnimSelector.WinLoseCheer.Cheer)
                {
                    player.transform.position = m_TopGameObj.transform.position;
                    player.transform.rotation = m_TopGameObj.transform.rotation;
                }
            }

            bSetPlayer = true;
        }
    }
    
}

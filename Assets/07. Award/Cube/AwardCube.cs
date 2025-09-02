using UnityEngine;

public class AwardCube : MonoBehaviour
{
    bool bSetPlayer = false;

    private const string TopGameObjName = "BoxTop";
    private GameObject m_TopGameObj;
    public enum AwardType
    {
        Gold,
        Silver,
        None
    }
    [SerializeField] private AwardType m_AwardType = AwardType.None;

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
            
            var (winner, loser) = GameSettings.Instance.GetWinnerLoser();
            IPlayerInfo.PlayerType MyType = IPlayerInfo.PlayerType.End;
            switch (m_AwardType)
            {
                case AwardType.Gold:
                    MyType = winner;
                    break;
                case AwardType.Silver:
                    MyType = loser;
                    break;
            }
            foreach (var player in players)
            {
                if (player.name.Contains("Monkey") && MyType == IPlayerInfo.PlayerType.Monkey)
                {
                    player.transform.position = m_TopGameObj.transform.position;
                    player.GetComponent<AwardAnimSelector>().SetWinLoseCheer(m_AwardType);
                }
                else if (player.name.Contains("Fox") && MyType == IPlayerInfo.PlayerType.Fox)
                {
                    player.transform.position = m_TopGameObj.transform.position;
                    player.GetComponent<AwardAnimSelector>().SetWinLoseCheer(m_AwardType);
                }
                else if (player.name.Contains("Turtle") && MyType == IPlayerInfo.PlayerType.Turtle)
                {
                    player.transform.position = m_TopGameObj.transform.position;
                    player.GetComponent<AwardAnimSelector>().SetWinLoseCheer(m_AwardType);
                }
            }

            bSetPlayer = true;
        }
    }

}

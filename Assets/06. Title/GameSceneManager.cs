using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameSceneManager : MonoBehaviour
{
    [Header("Scene Roots")]
    public Transform leftRoot;
    public Transform rightRoot;

    private System.Collections.IEnumerator Start()
    {
        // 한 프레임 대기: 씬 내 컴포넌트 초기화 보장
        yield return null;

        var gs = GameSettings.Instance;
        if (gs == null)
        {
            Debug.LogWarning("GameSettings 인스턴스가 없습니다.");
            yield break;
        }

        if (leftRoot == null || rightRoot == null)
        {
            Debug.LogWarning("leftRoot/rightRoot가 설정되어 있지 않습니다.");
            yield break;
        }

        // 슬롯→루트 매핑: 1vs1(P1→Left, P2→Right), 1vsCPU(P1→Left, CPU→Right)
        string leftSlot, rightSlot;
        if (gs.gameMode == "1vs1")
        {
            leftSlot = "P1";
            rightSlot = "P2";
        }
        else
        {
            leftSlot = "P1";
            rightSlot = "CPU";
        }

        // 선택된 id 조회 (+ 1vsCPU 폴백: Right가 비어있으면 Left 사용)
        string leftId = gs.GetCharacterForSlot(leftSlot);
        string rightId = gs.GetCharacterForSlot(rightSlot);
        if (gs.gameMode != "1vs1" && string.IsNullOrWhiteSpace(rightId))
            rightId = leftId;

        // 좌/우에서 선택된 캐릭터만 남기기(매칭 없으면 비활성화하지 않음)
        var leftKept = KeepOnlyCharacter(leftRoot, leftId);
        var rightKept = KeepOnlyCharacter(rightRoot, rightId);

        // 슬롯 기록 + 키바인딩 연결
        if (leftKept != null)
        {
            var reg = leftKept.GetComponentInChildren<PlayerInputRegistrar>(true);
            if (reg != null) reg.slot = leftSlot;
            BindPlayerInputToSlot(leftKept, leftSlot, gs);
        }
        if (rightKept != null)
        {
            var reg = rightKept.GetComponentInChildren<PlayerInputRegistrar>(true);
            if (reg != null) reg.slot = rightSlot;
            BindPlayerInputToSlot(rightKept, rightSlot, gs);
        }

        // 모든 슬롯 등록 후 저장된 키바인딩 재적용(보강)
        gs.LoadKeyBindings();
    }

    private static bool IdEquals(string a, string b)
    {
        return string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    // root의 직계 자식들 중 PlayerInputRegistrar.id가 원하는 것만 남기고 나머지 비활성화
    // 단, 매칭을 하나도 못 찾으면 아무 것도 비활성화하지 않고 경고만 출력
    private GameObject KeepOnlyCharacter(Transform root, string characterId)
    {
        if (root == null) return null;

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning($"[{root.name}] characterId가 비어 있습니다. 상태를 변경하지 않습니다.");
            // 첫 번째 활성 자식을 우선 반환(없으면 첫 자식)
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i).gameObject;
                if (child.activeSelf) return child;
            }
            return root.childCount > 0 ? root.GetChild(0).gameObject : null;
        }

        GameObject keep = null;
        var candidates = new List<(GameObject go, string id)>();

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i).gameObject;
            var reg = child.GetComponentInChildren<PlayerInputRegistrar>(true);
            var cid = reg != null ? reg.id : null;

            candidates.Add((child, cid));

            if (reg != null && IdEquals(cid, characterId))
            {
                keep = child;
            }
        }

        if (keep == null)
        {
            Debug.LogWarning($"[{root.name}] 일치하는 캐릭터(id='{characterId}')를 찾지 못했습니다. 비활성화를 건너뜁니다. 후보: {string.Join(", ", candidates.ConvertAll(c => string.IsNullOrEmpty(c.id) ? "(null)" : c.id))}");
            // 상태 변경 없이 첫 활성 자식 반환
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i).gameObject;
                if (child.activeSelf) return child;
            }
            return root.childCount > 0 ? root.GetChild(0).gameObject : null;
        }

        // 매칭 성공: 선택된 것만 활성, 나머지 비활성
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i).gameObject;
            child.SetActive(child == keep);
        }

        return keep;
    }

    // PlayerInput.actions를 슬롯별로 복제 후 등록(저장된 오버라이드 즉시 적용)
    private void BindPlayerInputToSlot(GameObject root, string slot, GameSettings gs)
    {
        var pi = root.GetComponentInChildren<PlayerInput>(true);
        if (pi == null || pi.actions == null) return;

        var cloned = ScriptableObject.Instantiate(pi.actions);
        cloned.name = $"{pi.actions.name}_{slot}_Runtime";
        pi.actions = cloned;

        gs.RegisterActionsForSlot(slot, cloned);

        // 확실히 활성화(재적용)
        cloned.Disable();
        cloned.Enable();

        // 디버그: 대표 액션 하나의 바인딩 확인
        var probe = cloned.FindAction("MoveLeft", false) ?? cloned.FindAction("Smash", false);
        if (probe != null && probe.bindings.Count > 0)
            Debug.Log($"[{slot}] {probe.name} = {probe.bindings[0].ToDisplayString()}");
    }
}
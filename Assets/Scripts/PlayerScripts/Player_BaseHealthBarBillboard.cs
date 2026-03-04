using UnityEngine;

[DisallowMultipleComponent]
public class Player_BaseHealthBarBillboard : MonoBehaviour
{
    [Tooltip("Transform do alvo (PlayerBase). Se vazio, será procurado num ancestor PlayerBase.")]
    public Transform target;

    [Tooltip("Offset em relação à base (em espaço local se for filha, ou world se não for).")]
    public Vector3 worldOffset = new Vector3(0f, 1.4f, 0f);

    private bool isChildOfTarget;
    private Vector3 initialLocalPos;

    void Start()
    {
        if (target == null)
        {
            var pb = GetComponentInParent<PlayerBase>();
            if (pb != null)
                target = pb.transform;
        }

        if (target == null)
        {
            Debug.LogWarning("[Player_BaseHealthBarBillboard] Target não encontrado.");
            enabled = false;
            return;
        }

        isChildOfTarget = transform.parent == target;

        if (isChildOfTarget)
        {
            initialLocalPos = transform.localPosition;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        if (isChildOfTarget)
        {
            transform.localPosition = initialLocalPos;
        }
        else
        {
            transform.position = target.position + worldOffset;
        }

        // Mantém a barra “reta” (sem rodar com a câmera)
        transform.rotation = Quaternion.identity;
    }
}
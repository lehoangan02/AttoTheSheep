using UnityEngine;

public class WoodcutterNPC : MonoBehaviour
{
    public enum WoodcutterState
    {
        Chopping,
        RunningToHouse,
        InHouse,
        RunningBackToTree
    }

    [Header("References")]
    public Transform targetTree;
    public Transform house;
    public Animator animator;
    public Renderer npcRenderer;

    [Header("Stats")]
    public float moveSpeed = 3f;
    public float chopDuration = 5f;
    public float houseStayDuration = 3f;

    private WoodcutterState currentState;
    private Vector3 initialPosition;
    private float currentTimer = 0f;

    void Start()
    {

        initialPosition = transform.position;

        ChangeState(WoodcutterState.Chopping);
    }

    void Update()
    {
        switch (currentState)
        {
            case WoodcutterState.Chopping:

                FaceTarget(targetTree.position);

                currentTimer += Time.deltaTime;
                if (currentTimer >= chopDuration)
                {
                    ChangeState(WoodcutterState.RunningToHouse);
                }
                break;

            case WoodcutterState.RunningToHouse:
                MoveTo(house.position);
                if (Vector2.Distance(transform.position, house.position) < 0.1f)
                {
                    ChangeState(WoodcutterState.InHouse);
                }
                break;

            case WoodcutterState.InHouse:
                currentTimer += Time.deltaTime;
                if (currentTimer >= houseStayDuration)
                {
                    ChangeState(WoodcutterState.RunningBackToTree);
                }
                break;

            case WoodcutterState.RunningBackToTree:
                MoveTo(initialPosition);
                if (Vector2.Distance(transform.position, initialPosition) < 0.1f)
                {
                    ChangeState(WoodcutterState.Chopping);
                }
                break;
        }
    }

    void MoveTo(Vector3 destination)
    {
        Vector3 targetPosition = new Vector3(destination.x, destination.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        FaceTarget(destination);
    }

    void FaceTarget(Vector3 targetPosition)
    {
        if (targetPosition.x > transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (targetPosition.x < transform.position.x)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    void ChangeState(WoodcutterState newState)
    {
        currentState = newState;
        currentTimer = 0f;

        if (npcRenderer != null)
        {
            npcRenderer.enabled = (currentState != WoodcutterState.InHouse);
        }

        animator.SetBool("IsChopping", false);
        animator.SetBool("IsCarrying", false);
        animator.SetBool("IsRunning", false);

        switch (currentState)
        {
            case WoodcutterState.Chopping:
                animator.SetBool("IsChopping", true);
                break;
            case WoodcutterState.RunningToHouse:
                animator.SetBool("IsCarrying", true);
                break;
            case WoodcutterState.RunningBackToTree:
                animator.SetBool("IsRunning", true);
                break;
        }
    }
}
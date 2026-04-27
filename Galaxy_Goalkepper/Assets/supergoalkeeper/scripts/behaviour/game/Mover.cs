using UnityEngine;


namespace supergoalkeeper
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Mover : MonoBehaviour
    {


        public float speed = 8f;
        public float curveStrength = 2f;
        public float spin = 2f;

        [HideInInspector] public MissionOne missionOne;

        private Rigidbody2D rb;
        private Transform goal;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        void Start()
        {
            CacheGoal();
        }

        void OnEnable()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody2D>();

            if (goal == null)
                CacheGoal();

            LaunchFromCurrentPosition();
        }

        private void CacheGoal()
        {
            GameObject goalObj = GameObject.FindGameObjectWithTag("Goal");
            if (goalObj != null)
            {
                goal = goalObj.transform;
            }
        }

        private void LaunchFromCurrentPosition()
        {
            if (goal == null || rb == null)
                return;

            Vector2 direction = (goal.position - transform.position).normalized;

            float curveDirection = Random.value < 0.5f ? -1f : 1f;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 curve = perpendicular * curveDirection * curveStrength;

            Vector2 finalVelocity = (direction + curve).normalized * speed;

            rb.linearVelocity = finalVelocity;
            rb.angularVelocity = spin * curveDirection;
        }

        public void RespawnBall()
        {
            if (missionOne == null)
                return;

            Transform spawnPoint = missionOne.GetRandomSpawnPoint();
            if (spawnPoint == null)
                return;

            transform.position = spawnPoint.position;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            LaunchFromCurrentPosition();
        }
    }

}
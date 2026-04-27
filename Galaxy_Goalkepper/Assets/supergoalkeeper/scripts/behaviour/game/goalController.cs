using System.Collections;
using UnityEngine;
namespace supergoalkeeper
{
    public class goalController : MonoBehaviour
    {


        public ParticlePool particlePool;
        public int blinkTimes = 3;
        public GameObject timer;
        public int goals = 0;


        /**
         * VARIABLES
         * */
        //public int blinkTimes = 3;//TIMER BLINK SET UP
        //public GameObject timer;         // GUITEXT TIME
        //public int goals = 0;


        //COROUTINE TIMER BLINK, TIMER BLINKS WHEN A GOAL IS SCORED
        //IEnumerator timerBlink()
        //{
        //	for(int i=0;i<3 ;i++)
        //	{
        /*this.timer.guiText.color=Color.red;*/
        //		yield return new WaitForSeconds(0.2f);
        /*this.timer.guiText.color=Color.white;*/
        //		yield return new WaitForSeconds(0.2f);
        //	}
        //}

        //WHEN A GOAL IS SCORED -1 SEC
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("balls"))
                return;

            if (particlePool != null)
            {
                GameObject fx = particlePool.GetParticle();
                if (fx != null)
                {
                    fx.transform.position = other.transform.position;
                    fx.SetActive(true);
                    StartCoroutine(DisableFX(fx));
                }
            }

            goals++;

            

            other.gameObject.SetActive(false);
        }

        IEnumerator DisableFX(GameObject fx)
        {
            yield return new WaitForSeconds(1f);
            if (fx != null)
                fx.SetActive(false);
        }
    }
}

using UnityEngine;

namespace MatchThemAll.Scripts
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class Item : MonoBehaviour
    {
        public void DisableShadow()
        {
            
        }
        
        public void DisablePhysics()
        {
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<SphereCollider>().enabled = false;
        }
    }
}
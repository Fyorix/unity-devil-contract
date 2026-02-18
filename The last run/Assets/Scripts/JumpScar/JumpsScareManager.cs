using UnityEngine;

public class JumpscareManager : MonoBehaviour
{
    // C'est ici qu'on glisse l'objet JUMPSCARE de la hiérarchie
    public GameObject jumpScareObject; 

    // Appelle cette fonction quand tu veux faire peur au joueur
    public void TriggerJumpScare()
    {
        if (jumpScareObject != null)
        {
            jumpScareObject.SetActive(true);
            // Optionnel : Désactiver après 2 secondes
            Invoke("HideJumpScare", 2.0f); 
        }
    }

    void HideJumpScare()
    {
        jumpScareObject.SetActive(false);
    }
}
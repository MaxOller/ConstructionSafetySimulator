﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;


// @TODO: Add a "Look At" point so the player is always facing the correct direction when they respawn
public class PlayerDeathHandler : MonoBehaviour
{
    public Material PlayerVisionFade;
    public int timeToDie; // 85 is a good value for this.
    public int timeToRespawn; // 100 is a good value for this.

    // these have to be public for some reason, or else we get an error in "AddStateToPlayer"
    // but you DO NOT need to add these in the inspector, the SceneRestore script will automatically add itself to this script.
    public List<GameObject> stateSavers;
    public List<SceneRestore> respawnPoints;

    int listSize;
    public bool callDeathCycleDirectly;     // allows you to test the script in the inspector

    Color VisionFadeColor;

    private void Start() {
        VisionFadeColor = new Color(1, 1, 1, 1);
        Shader.SetGlobalColor("_VisionFadeColor", VisionFadeColor); // finds the "_VisionFadeColor" variable located in any shader, and modifies it to the value of "VisionFadeColor"
    }

    // @TODO: Remove update method once testing is no longer required
    private void Update() {
        if (callDeathCycleDirectly) {  
            Debug.Log("CALLING DEATH CYCLE DIRECTLY");
            callDeathCycleDirectly = false;
            StartDeathCycle();
        }
    }


    public void AddStateToPlayer(SceneRestore resPoint) {   // allows a scene restoration script to be added to the handler
        stateSavers.Add(resPoint.gameObject);
        respawnPoints.Add(resPoint);
        listSize++;
    }

    public void RemoveStateFromPlayer(SceneRestore resPoint) { // allows a scene restoration script to be removed from the handler
        stateSavers.Remove(resPoint.gameObject);
        respawnPoints.Remove(resPoint);
        listSize--;
    }

    public void StartDeathCycle() { // the main functionality. Finds the closest respawn point in the list, and resets the associated scene, teleporting the player there as well
        Camera.main.GetComponent<CameraOverride>().EnableCameraOverride(PlayerVisionFade);  // enables camera override, so we can draw on top of the camera's view

        if (PlayerVisionFade == null) {
            Debug.LogError("Can't run death cycle on Player Object, PlayerVisionFade material not attached!");
            return;
        }

        float minDist = 99999;
        int currIndex = 0;
        int minDistIndex = 0;


        if (listSize > 0) {
            foreach (SceneRestore resPoint in respawnPoints) {
                float dist = 0;
                
                if(Camera.main.GetComponent<FallbackCameraController>() != null) {
                    dist = Vector3.Distance(resPoint.respawnPoint.position, Camera.main.transform.position);
                } else {
                    dist = Vector3.Distance(resPoint.respawnPoint.position, this.transform.position);
                }


                if (dist < minDist) {
                    minDist = dist;
                    minDistIndex = currIndex;
                }

                currIndex++;
            }
        }
        

        StartCoroutine("DeathCycle", minDistIndex); // we need a smooth fade, so we're going to run a coroutine. This is like creating a new 'thread'
    }


    IEnumerator DeathCycle(int minDistIndex) {
        float fadeColor = timeToDie;

        for (int i = 0; i < timeToDie; i++) {   // this loop runs to fade the vision to black
            fadeColor = fadeColor / timeToDie;
            //Debug.Log("COLOR: " + fadeColor);
            

            VisionFadeColor.r = fadeColor; VisionFadeColor.g = fadeColor; VisionFadeColor.b = fadeColor;
            //Debug.Log(VisionFadeColor.ToString());
            Shader.SetGlobalColor("_VisionFadeColor", VisionFadeColor);

            Debug.Log("DYING");
            fadeColor = fadeColor * timeToDie;
            fadeColor = fadeColor - 1;
            yield return new WaitForSeconds(.025f);
        }


        if(listSize > 0) {  // this loop teleports the player to the spawn points, and resets the scene they are being respawned at
            if (Camera.main.GetComponent<FallbackCameraController>() != null) { // if we're not using VR, we can just transport the camera directly where it needs to go
                Camera.main.transform.position = respawnPoints[minDistIndex].respawnPoint.position;
            } else {    // if we're using VR, we need to remove the y component, because our Player has no gravity physics
                Vector3 playPos = this.transform.position;
                playPos = respawnPoints[minDistIndex].respawnPoint.position;
                playPos.y = this.transform.position.y;
                this.transform.position = playPos;
            }

            respawnPoints[minDistIndex].ResetScene();
        }

        VisionFadeColor.r = 0; VisionFadeColor.g = 0; VisionFadeColor.b = 0;
        Shader.SetGlobalColor("_VisionFadeColor", VisionFadeColor);

        StartCoroutine("FixVision");
    }

    IEnumerator FixVision() {   // fades the vision back in
        StopCoroutine("DeathCycle");
        float fadeColor = 0;

        yield return new WaitForSeconds(1.5f);


        for (int i = 0; i < timeToRespawn; i++) {
            fadeColor = fadeColor / timeToRespawn;
            //Debug.Log("COLOR: " + fadeColor);

            VisionFadeColor.r = fadeColor; VisionFadeColor.g = fadeColor; VisionFadeColor.b = fadeColor;
            Shader.SetGlobalColor("_VisionFadeColor", VisionFadeColor);

            Debug.Log("RESPAWNING");
            fadeColor = fadeColor * timeToRespawn;
            fadeColor = fadeColor + 1;
            yield return new WaitForSeconds(.025f);
        }

        Camera.main.GetComponent<CameraOverride>().DisableCameraOverride(); // disables the camera override, so the result being drawn to the camera is unmodified

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing;

public class PlayerManager : SingletonMonoBehaviour<PlayerManager>
{
    enum GameState
    { Title, Main, Reslut };

    private GameState state = GameState.Title;

    [SerializeField]
    private PlayerDate date;

    private PlayerInput input;  // インプットの関数がある

    private PlayerTeleport move;    // テレポート移動の関数がある

    private DrawLine drawLine;  // 誘導線の関数がある

    private ControllerPull pull;    // 引いた時の関数がある

    private CursorPosition cursorFunction;

    private PlayerRunaway runaway;

    private DrawrRope rope;

    private PostProcessingBehaviour postProcess;

    private Gimmickbase gimmckScript;

    private AnchorShoot anchorAction;

    private CameraFade cameraFade;

    private PlayerMap playerMap;

    /*----------------------------------------------------------------------------------------------------------------*/
    private bool trigger = false;   // viveのトリガーのon,off

    private bool isTrigger = false;

    private bool gimmickIn = false;

    private bool otakaraIn = false;

    private bool otakaraSwich = false;

    private bool mapSwith = false;

    private bool anchorSwith = false;

    private bool moveStop = false;

    private bool resultSceneMove = false;

    private bool goleMapSwitch = false;

    private bool arrowAngleSwitch = false;

    private bool seSwitch = false;

    private bool efSwitch = false;

    /*----------------------------------------------------------------------------------------------------------------*/
    private Ray viveRay;

    private RaycastHit viveRayHit;
    /*----------------------------------------------------------------------------------------------------------------*/
    private Vector3 finishPosition;

    private Vector3 displayPosition;
    /*----------------------------------------------------------------------------------------------------------------*/
    private SteamVR_Controller.Device viveDevice;
    /*----------------------------------------------------------------------------------------------------------------*/
    private float time = 0;

    private float mapAlphaTime = 0;
    /*----------------------------------------------------------------------------------------------------------------*/
    [SerializeField]
    private LineRenderer line;  // 描画するLineRenderer

    [SerializeField]
    private GameObject wayPoint;    // 中間地点

    [SerializeField]
    private GameObject pullBody;

    [SerializeField]
    private Transform gole;

    [SerializeField]
    private GameObject anchor;

    [SerializeField]
    private GameObject gunSetAnchor;

    [SerializeField]
    private GameObject gunTrigger;

    [SerializeField]
    private GameObject map;

    [SerializeField]
    private GameObject arrowCursor;

    [SerializeField]
    private GameObject viveRightController; // viveコントローラー(Camera RigのModelをアタッチしてください)

    [SerializeField]
    private GameObject pointCursor, banCurcor; // カーソルをアタッチしてください

    [SerializeField]
    private GameObject player;  // CameraRig(Player)をアタッチしてください

    [SerializeField]
    private GameObject particle;    // 使用するパーティクルをアタッチしてください

    [SerializeField]
    GameObject eye; //Camera(eye)をアタッチしてください

    [SerializeField]
    PostProcessingProfile def;

    [SerializeField]
    PostProcessingProfile postMove;

    [SerializeField]
    private Material banLineMt, nomalLineMt, wireMt;

    [SerializeField]
    private GameObject fadeObject;

    [SerializeField]
    private Material mapMt;

    [SerializeField]
    private Transform playerPosition;

    [SerializeField]
    private ParticleSystem hibana;

    /*----------------------------------------------------------------------------------------------------------------*/
    private GameObject memoryObject;

    private GameObject otakara;

    private GameObject cursor;

    private Material memoryMt;

    private Material fadeMt;

    private Material lineMt;

    private Vector3 memoryPosition;    // 引く時のPositionを

    private void Start()
    {
        input = new PlayerInput();
        move = new PlayerTeleport();
        drawLine = new DrawLine(line);
        rope = new DrawrRope(line);
        pull = new ControllerPull();
        cursorFunction = new CursorPosition();
        runaway = new PlayerRunaway();
        anchorAction = new AnchorShoot();
        postProcess = eye.GetComponent<PostProcessingBehaviour>();
        cameraFade = new CameraFade();
        playerMap = new PlayerMap( );
        lineMt = line.GetComponent<Renderer>().material;

        banCurcor.SetActive(false);
        cursor = pointCursor;

        goleMapSwitch = true;

        arrowCursor.SetActive(false);

        hibana.gameObject.SetActive(false);

        pullBody.SetActive(false);
        map.SetActive(false);
        particle.SetActive(false);

        viveDevice = SteamVR_Controller.Input((int)viveRightController.GetComponent<SteamVR_RenderModel>().index);
        fadeMt = fadeObject.GetComponent<MeshRenderer>().material;
        FadeInStart();

    }

    /// <summary>
    /// 処理の流れ： 場所をサーチ→曲線描画or→引っ張るアクション→助走→テレポート
    /// </summary>
    private void Update()
    {

        if (state == GameState.Reslut)
        {
            time += Time.deltaTime;
            if (time <= 5) { return; }
            if (!resultSceneMove)
            {
                input.ResultViveInputtrigger(viveDevice, ref resultSceneMove, ref time);
                return;
                
            }
            SceneLoadManager.Instance.LoadScene("Stage");
            return;
        }

        if (state == GameState.Title)
        {
            bool changeSwich = false;
            changeSwich = input.TitleViveInputTrigger(viveDevice);

            if (changeSwich) { MainManager.Instance.TitleLogoEraseLoad(); }

            return;
        }

        if (!anchorSwith) {
            anchor.SetActive(false);
            gunSetAnchor.SetActive(true);
        }

        input.ViveInputMenuButton(viveDevice, ref map, ref mapSwith, ref mapAlphaTime, goleMapSwitch, ref arrowCursor, ref arrowAngleSwitch);

        if (mapSwith)
        {
            if (mapAlphaTime >= 1)
            {
                map.GetComponent<Renderer>().material.color = new Color(0, 0, 0, mapAlphaTime);
                mapAlphaTime += Time.deltaTime;
            }
        }
        
        if(arrowAngleSwitch)
        {
            playerMap.MapArrowAngle(map.transform, ref arrowCursor, gole);

        }

        // ギミックが入っていた時にviveコントローラーのパッドを押せば起動する
        if (gimmickIn)
        {
            input.ViveInputTouchpad(viveDevice, ref gimmckScript);
            gimmickIn = false;
            return;
        }

        if (otakaraIn)
        {
            otakaraSwich = input.ViveInputOtakaraGet(viveDevice, otakara, viveRightController.transform.position, ref memoryPosition);
        }

        // ここでviveのトリガー判定をしている
        isTrigger = input.ViveRockInputTrigger(viveDevice, viveRightController.transform.position, ref memoryPosition, ref pullBody, ref anchorSwith);

        // テレポート移動(ここに書いたのはこの後入力できないようにするため)
        if (trigger)
        {
            if (player.transform.position == finishPosition)
            {
                trigger = false;
                return;
            }

            particle.SetActive(true);

            //DOFとBlurを適用させる
            postProcess.profile = postMove;

            time += Time.deltaTime;

            viveDevice.TriggerHapticPulse(date.vibration);

            player.transform.position = runaway.Runaway(player.transform.position, finishPosition, date.moveSpeed, time);

            particle.transform.position = displayPosition;
            particle.transform.LookAt(player.transform.position);

            if (player.transform.position != finishPosition) { return; }

            pullBody.SetActive(false);

            particle.SetActive(false);

            

            hibana.gameObject.SetActive(false);

            efSwitch = false;

            line.startWidth = 0.2f;

            trigger = false;
            isTrigger = false;
            particle.SetActive(false);
            postProcess.profile = def;
            time = 0;
            return;

        }


        // ここで場所をサーチする
        if (!isTrigger && !otakaraSwich) { SearchRay(); }

        drawLine.OnDrawLine(displayPosition, date.hight, viveRightController.transform.position, date.maxDis);

        float dis = Vector3.Distance(pullBody.transform.position, viveRightController.transform.position);
        //Debug.Log(line.material);
        // サーチした場所が設定した値より遠い場合処理を止める
        dis = Vector3.Distance(displayPosition, viveRightController.transform.position);
        if (dis > date.maxDis || date.minDis > dis && !trigger && !isTrigger && !otakaraSwich || moveStop)
        {
            //line.GetComponent<MeshRenderer>().material = banLineMt;
            //lineMt = banLineMt;
            line.material = banLineMt;

            if (cursor != banCurcor)
            {
                pointCursor.SetActive(false);
                banCurcor.SetActive(true);
                cursor = banCurcor;
            }
            return;
        }

        //lineMt = nomalLineMt;
        //line.GetComponent<MeshRenderer>().material = nomalLineMt;
        line.material = nomalLineMt;
        if (cursor != pointCursor)
        {
            banCurcor.SetActive(false);
            pointCursor.SetActive(true);
            cursor = pointCursor;
        }

        // もしトリガーを引いていなかったら曲線を出す
        if (!trigger && !isTrigger && !otakaraSwich && !moveStop)
        {
            gunTrigger.transform.rotation = Quaternion.AngleAxis(0, new Vector3(1, 0, 0));
            if(line.startWidth <= 0.2f)
            {
                line.startWidth = 0.2f;
            }
            return;
        }

        // 縄表示
        drawLine.SetRope(anchor.transform.position, date.hight, viveRightController.transform.position, dis);
        //lineMt = wireMt;
        //line.GetComponent<MeshRenderer>().material = wireMt;
        line.material = wireMt;
        line.startWidth = 0.05f;
        if (displayPosition != anchor.transform.position)
        {
            if (!anchorSwith) { anchorSwith = true; }
            gunTrigger.transform.rotation = Quaternion.AngleAxis(30, new Vector3(1, 0, 0));
            gunSetAnchor.SetActive(false);
            anchor.SetActive(true);
            anchor.transform.position = anchorAction.AnchorMove(viveRightController.transform.position, displayPosition, time);
            time += Time.deltaTime * 3;
            
            if(efSwitch && !otakaraIn)
            {
                hibana.Stop();
                hibana.time = 0;
                efSwitch = false;
            }

            return;
        }

        if(!efSwitch&&!otakaraIn)
        {
            hibana.gameObject.transform.position = anchor.transform.position;
            hibana.gameObject.SetActive(true);
            hibana.Play();
            efSwitch = true;
        }

        time = 0;

        if (trigger) { return; }

        // 引っ張るアクションをしている

        pullBody.SetActive(true);
    }

    /// <summary>
    /// Rayを飛ばし当ったオブジェクトのpointPositionを受け取る
    /// </summary>
    private void SearchRay()
    {
        viveRay = new Ray(viveRightController.transform.position, viveRightController.transform.forward);
        if (Physics.Raycast(viveRay, out viveRayHit))
        {
            HitSearch(viveRayHit);
            wayPoint.transform.position = new Vector3((this.transform.position.x + finishPosition.x) * 0.5f, (this.transform.position.y + finishPosition.y) * 0.5f, (this.transform.position.z + finishPosition.z) * 0.5f);
            cursorFunction.Cursor(ref cursor, viveRayHit, displayPosition, player.transform.position, date.maxDis);
        }
        else
        {
            gimmickIn = false;
            otakaraIn = false;
            return;
        }

        wayPoint.transform.LookAt(finishPosition);
        float dis = Vector3.Distance(wayPoint.transform.position, finishPosition);

        Ray ray = new Ray();
        RaycastHit rayHit;

        if (Physics.Raycast(ray, out rayHit, dis))
        {
            HitSearch(rayHit);
            cursorFunction.Cursor(ref cursor, rayHit, displayPosition, player.transform.position, date.maxDis);
        }

        wayPoint.transform.LookAt(this.transform.position);
        if (Physics.Raycast(ray, out rayHit, dis))
        {
            HitSearch(rayHit);
            cursorFunction.Cursor(ref cursor, rayHit, displayPosition, player.transform.position, date.maxDis);
        }
    }

    /// <summary>
    /// Rayが当たった場合の処理
    /// </summary>
    /// <param name="hit"></param>
    private void HitSearch(RaycastHit hit)
    {

        if (hit.collider.tag == "Player") { return; }
        if (hit.collider.tag == "Wall")
        {

            displayPosition = hit.point;
            moveStop = true;
            return;
        }


        if (hit.collider.tag == "Okimono")
        {
            //memoryObject = hit.transform.gameObject;

            foreach (Transform transform in hit.collider.gameObject.transform)
            {
                finishPosition = transform.position;
            }
            moveStop = false;
            displayPosition = hit.point;
            return;
        }

        float distans = 2.5f;

        if (Vector3.Distance(player.transform.position, hit.point) <= 5) { distans = 0.5f; }

        if (hit.collider.tag == "Floor") { distans = 0; }

        finishPosition = hit.point + hit.normal * distans;

        displayPosition = hit.point;

        if (moveStop) { moveStop = false; }

        if (hit.collider.tag == "gimmick")
        {
            gimmckScript = hit.transform.gameObject.GetComponent<Gimmickbase>();
            gimmickIn = true;
        }

        if (hit.collider.tag == "Otakara")
        {
            otakara = hit.collider.gameObject;
            otakaraIn = true;
        }
    }

    public void PlayerSetposition(Vector3 setPosition)
    {
        player.transform.position = setPosition;
        time = 0;
    }

    public void ChangeState()
    {
        switch (state)
        {
            case GameState.Title:
                state = GameState.Main;
                break;

            case GameState.Main:
                state = GameState.Reslut;
                break;

            case GameState.Reslut:
                state = GameState.Title;
                break;
        }
    }

    public void PullOK()
    {
        if (otakaraSwich)
        {
            otakara.SetActive(false);
            otakaraIn = false;
            otakaraSwich = false;
            anchorSwith = false;
            mapSwith = true;
            goleMapSwitch = true;
            //map.GetComponent<MeshRenderer>().material = mapMt;

            MainManager.Instance.GoleDisplay();
            return;
        }

        trigger = true;
    }

    public void FadeInStart()
    {
        StartCoroutine("CameraFadeIn");
    }

    public void FadeOutStart()
    {
        StartCoroutine("CameraFadeOut");
    }

    private IEnumerator CameraFadeIn()
    {
        while(fadeMt.color.a > 0)
        {
            fadeMt.SetColor("_Color", cameraFade.FadeIn(fadeMt.color, date.fadeSpeed));
            yield return null ;
        }

        yield return null;
    }

    private IEnumerator CameraFadeOut()
    {
        while (fadeMt.color.a < 1)
        {
            fadeMt.SetColor("_Color", cameraFade.FadeOut(fadeMt.color, date.fadeSpeed));
            yield return null;
        }

        yield return null;
    }
}

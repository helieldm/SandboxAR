using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ZXing;
using ZXing.QrCode;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[Serializable]
public class QRCodeEventArgs<TData> : EventArgs
{
    public TData Data { get; private set; }

    public QRCodeEventArgs(TData data)
    {
        Data = data;
    }
}
public class QRCodeManager : MonoBehaviour
{
 
    public CanvasManager canvas;
    public GameObject objectToSpawn; //Reference to the AR Gameobject which needs to be placed on scanning the QRCode
    IBarcodeReaderGeneric reader; //QRCode reading library
    ARCameraManager aRCamera;
    ARRaycastManager arRaycastManager;
    private Texture2D arCameraTexture; //Texture to hold the processed AR Camera frame
    private bool onlyOnce;
    void Start()
{
        aRCamera = FindObjectOfType<ARCameraManager>(); //Load the ARCamera
        arRaycastManager = FindObjectOfType<ARRaycastManager>(); //Load the Raycast Manager
                                                                 //Get the ZXing Barcode/QRCode reader
        reader = new BarcodeReaderGeneric();
        //Subscribe to read AR camera frames: Make sure this statement runs only once
        aRCamera.frameReceived += OnCameraFrameReceived;
        Debug.Log("Logger works");

    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if ((Time.frameCount % 15) == 0)
        { //You can set this number based on the frequency to scan the QRCode
            XRCpuImage image;
            if (aRCamera.TryAcquireLatestCpuImage(out image))
            {
                StartCoroutine(ProcessQRCode(image));
                image.Dispose();
            }
        }
    }


    //Asynchronously Convert to Grayscale and Color : https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@1.0/manual/cpu-camera-image.html
    IEnumerator ProcessQRCode(XRCpuImage image)
    {
        // Create the async conversion request
        var request = image.ConvertAsync( new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
            // Color image format
            outputFormat = TextureFormat.RGB24,
            // Flip across the Y axis
            //  transformation = CameraImageTransformation.MirrorY
        });
        while (!request.status.IsDone())
            yield return null;
        // Check status to see if it completed successfully.
        if (request.status != XRCpuImage.AsyncConversionStatus.Ready)
        {
            // Something went wrong
            Debug.LogErrorFormat("Request failed with status {0}", request.status);
            // Dispose even if there is an error.
            request.Dispose();
            yield break;
        }
        // Image data is ready. Let's apply it to a Texture2D.
        var rawData = request.GetData<byte>();
        // Create a texture if necessary
        if (arCameraTexture == null)
        {
            arCameraTexture = new Texture2D(
            request.conversionParams.outputDimensions.x,
            request.conversionParams.outputDimensions.y,
            request.conversionParams.outputFormat,
            false);
        }
        // Copy the image data into the texture
        arCameraTexture.LoadRawTextureData(rawData);
        arCameraTexture.Apply();
        byte[] barcodeBitmap = arCameraTexture.GetRawTextureData();
        LuminanceSource source = new RGBLuminanceSource(barcodeBitmap, arCameraTexture.width, arCameraTexture.height);
        //Send the source to decode the QRCode using ZXing
        Result result;
        if (!onlyOnce)
        { //Check if a frame is already being decoded for QRCode. If not, get inside the block.
            onlyOnce = true; //Now frame is being decoded for a QRCode
                           //decode QR Code
            result = reader.Decode(source);
            if (result != null && result.Text != "")
            { //If QRCode found inside the frame
                Debug.Log("QR code not null?");
                canvas.DisplayPopup();
                string QRContents = result.Text;
                // Get the resultsPoints of each qr code contain the following points in the following order: index 0: bottomLeft index 1: topLeft index 2: topRight
                //Note this depends on the oreintation of the QRCode. The below part is mainly finding the mid of the QRCode using result points and making a raycast hit from that pose.
                ResultPoint[] resultPoints = result.ResultPoints;
                ResultPoint a = resultPoints[1];
                ResultPoint b = resultPoints[2];
                ResultPoint c = resultPoints[0];
                Vector2 pos1 = new Vector2((float)a.X, (float)a.Y);
                Vector2 pos2 = new Vector2((float)b.X, (float)b.Y);
                Vector2 pos3 = new Vector2((float)c.X, (float)c.Y);
                Vector2 pos4 = new Vector2(((float)b.X - (float)a.X) / 2.0f, ((float)c.Y - (float)a.Y) / 2.0f);
                List<ARRaycastHit> aRRaycastHits = new List<ARRaycastHit>();
                //Make a raycast hit to get the pose of the QRCode detected to place an object around it.
                if (arRaycastManager.Raycast(new Vector2(pos4.x, pos4.y), aRRaycastHits, TrackableType.FeaturePoint) && aRRaycastHits.Count > 0)
                {
                    Debug.Log("Raycast hit!");

                    //To shift the object to a relative position by adding/subtracting a delta value, uncomment the below line.
                    //Instantiate an object at Hitpose found on the QRCode
                    Transform spawnTransform = transform.parent;
                    spawnTransform.position = aRRaycastHits[0].pose.position;
                    GameObject instance = Instantiate(objectToSpawn, spawnTransform);
                    Debug.Log(instance.transform);
                    //OR
                    // Use default position to place the object in front of the camera if the Hit Pose is not found. //You can uncomment the below code for this default behaviour
                    //defaultObjectPosition = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, //Screen.height / 2, Camera.main.nearClipPlane));
                    //OR
                    //Reposition the Augmented Object by adding some delta
                    //NewObjectToPlace.transform.position = new //Vector3(NewObjectToPlace.transform.position.x + xDelta, //NewObjectToPlace.transform.position.y, NewObjectToPlace.transform.position.z);
                }
                else
                {
                    onlyOnce = false; //Continue processing the next frame to decoded for a QRCode if hit not found
                }
            }
            else
            {
                onlyOnce = false;  //QRCode not found in the frame. Continue processing next frame for QRCode
            }
        }
    }

    /*
    private QRCodeWatcher qrTracker;
    public event EventHandler<QRCodeEventArgs<QRCode>> QRCodeAdded;
    public event EventHandler<QRCodeEventArgs<QRCode>> QRCodeUpdated;
    public event EventHandler<QRCodeEventArgs<QRCode>> QRCodeRemoved;
    // Start is called before the first frame update
    void Start()
    {
        qrTracker = new QRCodeWatcher();
        qrTracker.Start();
        qrTracker.Added += QRCodeWatcher_Added;
    }

    public void QRCodeWatcher_Added(object sender, QRCodeAddedEventArgs args)
    {
        Debug.Log("QR CODE ADDED???/");
        canvas.DisplayPopup();
    }

    // Update is called once per frame
    void Update()
    {

    }
*/
}

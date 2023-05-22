using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Networking;
using System.Numerics;
using UnityEngine.SceneManagement;

public class UIClickHandler : MonoBehaviour
{
    string ASRURL = "http://43.154.157.60:8000/apis/ASR";
    string CHATCOMPLETIONURL = "http://43.154.157.60:8000/apis/chatCompletion";
    string CHECKFILEURL = "http://43.154.157.60:8000/apis/checkFile";

    string DOWNLOADAUDIOURL = "http://43.154.157.60:4203/";

    // Start is called before the first frame update

    // bool startedInterview = false;
    [SerializeField] TMP_Text stateTextEl;
    private AudioSource audioSource;
    public string deviceName;

    bool initStart = true;

    double [] XInput;
    double [] YInput;
    double [] YOutput;


    string initUtterance = "I want you to be an interviewer for a human resources position. I will be a candidate and you will ask me interview questions for human resource positions. I want you to answer only as an interviewer. Don't write all the questions at once. I want you to only interview me. Ask me questions, wait for my answer, and give me some comments. Don't write explanations. Ask me one by one like an interviewer, and wait for my answer. Remember to limit your response less than 30 words and use 'human resource' rather tha 'HR'! My first words are 'Hello interviewer'";

    // private string transcript = "Thank you for your introduction. Can you tell me why you want to join our company's human resources department? What do you know about the responsibilities and job content of human resources?Thank you for your introduction. Can you tell me why you want to join our company's human resources department? What do you know about the responsibilities and job content of human resources?";
    private string transcript = "";
    // The filename of the output audio file for current conversation round
    private string audioFilename;

    private string botResponse;
    private int SAMPLEWINDOW = 256;


    private AudioClip audioClip;
    


    void Start()
    {
        deviceName = Microphone.devices[0];
        audioSource = GetComponent<AudioSource>();
        stateTextEl.text = "";
        

        XInput = new double [SAMPLEWINDOW];
        YInput = new double [SAMPLEWINDOW];
        YOutput = new double [SAMPLEWINDOW];
        int maxFreq;
        int minFreq;
        Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
        print("min max frequency");
        print(minFreq);
        print(maxFreq);
        print(AudioSettings.outputSampleRate);
        print(deviceName);

        
        

    }

    // Update is called once per frame
    void Update()
    {

        if(!(Microphone.GetPosition(deviceName) > 0)) return;

        // analyzeSound();
        // visualizeSound();
        
    }

    // public void onRecordClicked(){
    //     if(!isRecording){
    //         isRecording = true;
    //         recordStatusText.text = "Recording...";
    //         audioClip = Microphone.Start(deviceName, true, 10, 44100);
    //         audioSource.clip = audioClip;
    //         // audioSource.Play();
    //         Debug.Log(deviceName);
    //     }
    //     else{
    //         isRecording = false;
    //         recordStatusText.text = "Recording Ended.";
    //         Microphone.End(deviceName);

            
    //         byte[] audioData = ConvertAudioClipToByteArray(audioClip);
    //         StartCoroutine(SendAudioToBackend(audioData));
    //     }

    // }
    private byte[] ConvertAudioClipToByteArray(AudioClip audioClip)
    {
        float[] audioData = new float[audioClip.samples * audioClip.channels];
        short [] audioDataShort = new short[audioData.Length];
        
        audioClip.GetData(audioData, 0);
        for (int i = 0; i < audioData.Length; ++i){
            audioDataShort[i] = Convert.ToInt16(audioData[i]*32767);
            if(i == 2){print(audioDataShort[i]);print(audioData[i]);}
        }
        byte[] byteArray = new byte[audioDataShort.Length * 2];
        Buffer.BlockCopy(audioDataShort, 0, byteArray, 0, byteArray.Length);
        return byteArray;
    }
    [System.Serializable]
    public class TranscriptContainer {
        public string transcript;
    }
    [System.Serializable]
    public class chatContainer{
        public string botResponse;
        public string filename;
        
    }
    private IEnumerator SendAudioToBackend(byte[] audioData)
    {

        
        // Send the form data to the Django backend server
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioData, "input.wav");
        UnityWebRequest www = UnityWebRequest.Post(ASRURL, form);
        
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Audio file sent successfully");

            transcript = JsonUtility.FromJson<TranscriptContainer>(www.downloadHandler.text).transcript;
            
            // add line breaks for beter formatting on the unity GUI
            string [] transcriptWords = transcript.Split(" ");
            transcript = "";
            for(int i = 0; i < transcriptWords.Length; ++i){
                transcript += transcriptWords[i];
                if (i % 8 == 0 && i != 0){
                    transcript += "\n";
                }
                else{
                    transcript += " ";
                }
                
            }
            
            
            Debug.Log(transcript);
            stateTextEl.text = "";

        }
        else
        {
            Debug.LogError("Failed to send audio file: " + www.error);
        }
        www.Dispose();
    
    }

    private IEnumerator SendTextToBackend(string text)
    {

        
        // Send the form data to the Django backend server
        WWWForm form = new WWWForm();
        form.AddField("userMessage", text);
        UnityWebRequest www = UnityWebRequest.Post(CHATCOMPLETIONURL, form);
        
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Text sent successfully");

            // transcript = JsonUtility.FromJson<TranscriptContainer>(www.downloadHandler.text).transcript;
            // audioSource.Play();
            audioFilename = JsonUtility.FromJson<chatContainer>(www.downloadHandler.text).filename;
            botResponse = JsonUtility.FromJson<chatContainer>(www.downloadHandler.text).botResponse;

            StartCoroutine(CheckFileExist(audioFilename));

        }
        else
        {
            Debug.LogError("Failed to send text: " + www.error);
        }
        www.Dispose();
    
    }
    
    private IEnumerator CheckFileExist(string filename){
        while(true){
            WWWForm form = new WWWForm();
            form.AddField("filename", filename);
            UnityWebRequest www = UnityWebRequest.Post(CHECKFILEURL, form);

            yield return www.SendWebRequest();

            if(www.result == UnityWebRequest.Result.Success){
                string checkResult = www.downloadHandler.text;
                print(checkResult);
                if(checkResult == "File exists."){
                    // start download audio here
                    StartCoroutine(downloadAudio(filename));
                    break;
                }
                else{
                    www.Dispose();
                }
                
            }
            www.Dispose();
            yield return new WaitForSeconds(1.0f);
        }
        
    }

    private IEnumerator downloadAudio(string filename){
        print("finding filename: " + filename);
        // using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("http://43.154.157.60:4203/download?filename="+filename+".wav", AudioType.WAV))
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(DOWNLOADAUDIOURL+filename, AudioType.WAV))
        {
            
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
            }
            else
            {
                AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = myClip;
                audioSource.Play();
                stateTextEl.text = "";
            }
        }
        
    }

    void OnGUI(){
        GUIStyle guiStyle = new GUIStyle(GUI.skin.button);
        guiStyle.fontSize = 50;
        if(initStart){
            if(GUI.Button(new Rect(100, 100, 300, 100), "Start", guiStyle)){
                StartCoroutine(SendTextToBackend(initUtterance));
                initStart = false;
            }
            return;
            
        }

        if(!Microphone.IsRecording(deviceName)){
            
            if(GUI.Button(new Rect(100, 100, 300, 100), "Record", guiStyle)){
                audioClip = Microphone.Start(deviceName, true, 20, AudioSettings.outputSampleRate);
                // animate the text "Listening......"
                stateTextEl.text = "Listening";
                
                // audioSource.Play();
            }

        }
        else{
            if(GUI.Button(new Rect(100, 100, 300, 100), "Stop", guiStyle)){
                while(!(Microphone.GetPosition(deviceName) > 0)){}
                Microphone.End(deviceName);
                byte[] audioData = ConvertAudioClipToByteArray(audioClip);


                stateTextEl.text = "Transcribing";

                StartCoroutine(SendAudioToBackend(audioData));
                
            }
            
        }
        
        if(GUI.Button(new Rect(500, 100, 300, 100), "Send", guiStyle)){
            // send chatCompletion request to the backend
            
            if(transcript != ""){
                StartCoroutine(SendTextToBackend(transcript));
                transcript = "";
                stateTextEl.text = "Thinking";
            }
            // StartCoroutine(downloadAudio("d"));
            
        }
        // transcript = "Hi, about my previous work experience,\n I have worked as human resource in HKUST company.\n And my job was mainly on \nmaking interview appointment with applicants.";
        if(transcript != ""){
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 25;
            GUIContent transcriptContent = new GUIContent(transcript);
            UnityEngine.Vector2 boxSize = GUI.skin.box.CalcSize(transcriptContent);
            GUI.Box(new Rect(100, 400, 800, 400), transcript, boxStyle);
        }
    }



    void FixedUpdate(){
        if(stateTextEl.text != ""){
            stateTextEl.text += ".";
            int tempLength = stateTextEl.text.Length;
            
            if(stateTextEl.text[tempLength-6].Equals("."[0])){
                stateTextEl.text = stateTextEl.text.Substring(0, tempLength - 6);
            }
        }
    }

    /*
    The following commented code was originally for realtime speech recognition.
    However, it seems hard and unmature for realtime speech recognition for unity.

    Two methods:
    1. Send the byte stream to a backend (whether third-party or not) via websocket connection
    2. Leverage the Azure Speech SDK for Csharp (needs to be adapted to use in Unity)

    By the way, the second method should follow the same logic as the first one at the very low level, only
    with different coding implementation.
    */


    // private void analyzeSound(){
    //     // float [] waveData = new float[SAMPLEWINDOW];
    //     // int micPosition = Microphone.GetPosition(deviceName) - (SAMPLEWINDOW + 1);
    //     // audioClip.GetData(waveData, micPosition);



    //     // int DB = 0;
    //     // float chunkSqrtMean = 0;
    //     // for (int i = 0 ; i < SAMPLEWINDOW; ++i){
    //     //     chunkSqrtMean += waveData[i] * waveData[i];
    //     // }
    //     // chunkSqrtMean = Mathf.Sqrt(chunkSqrtMean/SAMPLEWINDOW);
    //     // DB = (int)(20 * Mathf.Log10(chunkSqrtMean/0.1f));
    //     // Debug.Log("Current volume is: " + DB);
    //     float [] _samples = new float [SAMPLEWINDOW];
    //     GetComponent<AudioSource>().GetOutputData(_samples, 0); // fill array with samples
  
    //     float sum = 0;
    //     for (int i = 0; i < SAMPLEWINDOW; i++)
    //     {
    //         sum += _samples[i] * _samples[i]; // sum squared samples
    //     }
    //     float rmsVal = Mathf.Sqrt(sum / SAMPLEWINDOW); // rms = square root of average
    //     float dbVal = 20 * Mathf.Log10(rmsVal / 1); // calculate dB
    //     if (dbVal < -160) dbVal = -160;

    //     Debug.Log("DDDD" + _samples[100]);



    //     float [] spectrum = new float [SAMPLEWINDOW];
    //     audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
    //     print(spectrum[100]);
    //     print(audioSource.clip.name);
    //     float maxV = 0;
    //     int maxN = 0;
    //     for (int i=0; i < SAMPLEWINDOW; i++){ // find max 
    //         if (spectrum[i] > maxV && spectrum[i] > 0.02){
    //             maxV = spectrum[i];
    //             maxN = i; // maxN is the index of max
    //         }
    //     }
    //     float freqN = maxN; // pass the index to a float variable
    //     if (maxN > 0 && maxN < SAMPLEWINDOW-1){ // interpolate index using neighbours
    //         var dL = spectrum[maxN-1]/spectrum[maxN];
    //         var dR = spectrum[maxN+1]/spectrum[maxN];
    //         freqN += 0.5f*(dR*dR - dL*dL);
    //     }
    //     float pitchValue;
    //     pitchValue = freqN*(44100/2)/SAMPLEWINDOW; // convert index to frequency
    //     Debug.Log("FRE: " + pitchValue);
    // }

    // void visualizeSound(){
    //     print("Enter");
    //     if(Microphone.IsRecording(deviceName)){
    //         float [] samples= new float[SAMPLEWINDOW];

    //         audioClip.GetData(samples, 0);

    //         for(int i = 0; i < SAMPLEWINDOW; ++i){
    //             XInput[i] = i;
    //             YInput[i] = (double)(samples[i] * 32767);
    //             print("INPUT: "+ YInput[i]);
    //         }

    //         Complex [] inputSignalTime = new Complex [SAMPLEWINDOW];
    //         Complex [] outputSignalFreq = new Complex [SAMPLEWINDOW];

    //         inputSignalTime = FastFourierTransform.doubleToComplex(YInput);

    //         outputSignalFreq = FastFourierTransform.FFT(inputSignalTime, false);

    //         for(int i = 0; i < SAMPLEWINDOW; ++i){
    //             YOutput[i] = (double)Complex.Abs(outputSignalFreq[i]);
    //         }

    //         double maxY = 0;
    //         for (int i = 0; i < SAMPLEWINDOW; ++i){
    //             if(YOutput[i] > maxY){
    //                 maxY = YOutput[i];
    //             }
    //         }
            
    //         // Debug.Log("Frequency: " + maxY);
    //         // yield return null;
    //     }
        
    // }

}

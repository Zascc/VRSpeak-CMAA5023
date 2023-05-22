
let conversationHistory = []

function initAudioRecordHandler(){
  const constrains = {audio: true, video: false}
  navigator.mediaDevices.getUserMedia(constrains).then(function(stream) {
    const audioRecorder = new MediaRecorder(stream)
    const startRecordButton = document.getElementById('start-record-button')
    const stopRecordButton = document.getElementById('stop-record-button')


    startRecordButton.addEventListener('click', function (e){
      if(!(audioRecorder.state === 'recording')){
        audioRecorder.start()
      }
    })

    stopRecordButton.addEventListener('click', function (e){
      if(audioRecorder.state === 'recording'){
        audioRecorder.stop()
      }
    })

    // when record data chunks available, push them to the dataArray
    let dataArrary = []
    audioRecorder.ondataavailable = function (e) {
      console.log("I AM FUCKING AVAILABLE NOW")
      dataArrary.push(e.data)
    }

    // when audioRecorder is stopped, call the STT api to transcribe texts,
    // and show the transcription in the frontend
    audioRecorder.onstop = function (e) {

      const audioData = new Blob(dataArrary, {'type': 'audio/mp3;'})

      dataArrary = []

      const formData = new FormData()

      // formData.append("audio", audioData, "speech.mp3")
      formData.append("audio", audioData)
      console.log(formData)
      fetch("http:localhost:8000/apis/ASR", {
        method: 'POST',
        body: formData
      })
      .then(response => response.json())
      .then(data => {
        document.querySelector("#transcription-text").textContent = data['transcript']
        console.log(data['transcript'])
      })
    }
  })
  .catch(function(err) {
    console.log(err.name + ": " + err.message);
  });
}

function onSendButtonClicked(){
  const sendButton = document.querySelector("#send-button")

  // post to another endpoint
  const currentRoundText = document.querySelector("#transcription-text").textContent
  conversationHistory.push({"user_utter": currentRoundText})
  

  fetch("http:localhost:8000/apis/chatCompletion", {
    method: 'POST',
    headers: {
      'Content-Type': "application/json"
    },
    body: JSON.stringify(conversationHistory)
  })
  .then(response => response.json())
  .then(jsonData => {
    const botResponse = jsonData['botResponse']
    console.log(botResponse)
    const botResponseContainer = document.querySelector("#bot-response-text")
    botResponseContainer.textContent = botResponse

    const audioFileName = jsonData['fileName']
    const audioEl = new Audio(`../audios/${audioFileName}`)
    audioEl.play()
  })

  
}


document.addEventListener('DOMContentLoaded', async () => {
  initAudioRecordHandler()

  
})

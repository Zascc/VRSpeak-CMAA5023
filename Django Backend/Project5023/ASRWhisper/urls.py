from django.urls import path

from . import views

urlpatterns = [
    path("ASR", views.ASR, name="ASR"),
    path("chatCompletion", views.chatCompletion, name="chatCompletion"),
    path("checkFile", views.checkFile, name="checkFile"),
]
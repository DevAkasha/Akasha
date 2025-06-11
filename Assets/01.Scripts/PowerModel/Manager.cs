using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Manager<T> : Singleton<T> where T: Singleton<T>
{
    //게임매니저에서 관리 받는 내용 추가
    //라이프사이클 후크 받기(생성, 씬아웃, 씬로드, 소멸)
}
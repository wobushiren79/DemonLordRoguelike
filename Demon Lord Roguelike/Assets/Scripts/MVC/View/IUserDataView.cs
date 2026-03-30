/*
* FileName: UserData 
* Author: AppleCoffee 
* CreateTime: 2024-07-16-17:44:25 
*/

using UnityEngine;
using System;
using System.Collections.Generic;

public interface IUserDataView
{
	void GetUserDataSuccess<T>(T data, Action<T> action);

	void GetUserDataFail(string failMsg, Action action);

    void SetUserDataSuccess<T>(T data, Action<T> action);

    void SetUserDataFail(string failMsg);
}
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LYP.UICodeBuilder
{
	public class UICodeBuilderConfig : MonoBehaviour
	{
		public enum EventWidgetType
		{
			Button,
			Toggle,
			Slider,
			InputField,
			ScrollRect,
			Scrollbar,
			Dropdown,
		}

		public enum SuperclassIndex
		{
			MonoBehaviour,
			BaseView,
			NONE
		}

		public enum InterfaceIndex
		{
			ViewHolder
		}

		public static Dictionary<string, string> eventDelegateParamDic = new Dictionary<string, string>
		{
			{ "Toggle", "bool" },
			{ "Slider", "float" },
			{ "InputField", "string" },
			{ "ScrollRect", "Vector2" },
			{ "Scrollbar", "float" },
			{ "Dropdown", "int" },
		};

		#region cs代码格式
		public static string codeAnnotation =
@"/**
 * Powered by UICodeBuilder.
 * Author : " + System.Environment.UserName + @"
 * Date   : " + System.DateTime.Now.ToString("yyyy-MM-dd") + @"
 * Time   : " + System.DateTime.Now.ToString("t") + @"
 * Description:
 * (This file is auto generated by UICodeBuilder tool. You can edit anywhere.)
 */";
		public const string regionStartFmt = "\n\t#region {0}\n";
		public const string regionEnd = "\t#endregion\n";

		public static string statementRegion = string.Format(regionStartFmt, "UI Variable Statement");
		public static string eventRegion = string.Format(regionStartFmt, "UI Event Register");
		public static string assignRegion = string.Format(regionStartFmt, "UI Variable Assignment");

		public const string methodStartFmt = "\tprivate void {0}()\n\t{{\n"; //'{'要转义
		public const string methodEnd = "\n\t}\n";

		public const string ifGoStartFmt = "\t\tif (go == {0}.gameObject)\n\t\t{{";
		public const string ifelseGoStartFmt = "\n\t\telse if (go == {0}.gameObject)\n\t\t{{";
		public const string ifEnd = "\n\t\t}";

		public const string usingNamespace = "\nusing System.Collections;\nusing System.Collections.Generic;\nusing UnityEngine;\nusing UnityEngine.UI;\n";
		public const string usingBaseViewNamespace = "using LYP.UI;\n";
		public const string classMonoStart = "\npublic class {0} : MonoBehaviour{1}\n{{\n";
		public const string classBaseViewStart = "\npublic class {0} : BaseView{1}\n{{\n";
		public const string classStart = "\npublic class {0}{1}\n{{\n";
		public const string classEnd = "\n}\n";
		public const string methodAnnotation = "\n\t/// <summary>\n\t/// {0}\n\t/// </summary>\n";

		#region 序列化初始化代码格式
		//控件遍历声明,0:类型 1:名称
		public const string serilStateCodePrivateFmt = "\t[SerializeField] private {0} {1};\n";
		public const string serilStateCodePublicFmt = "\tpublic {0} {1};\n";

		public const string onClickSerilCode = "\t\t{0}.onClick.AddListener(On{1}Clicked);\n";
		public const string onClickSerilCodeForBaseView = "\t\tEventTriggerListener.Get({0}).onClick += OnBtnClick;\n";
		public const string onValueChangeSerilCode = "\n\t\t{0}.onValueChanged.AddListener(On{1}ValueChanged);";

		public const string btnCallbackSerilCode = "\n\tprivate void On{0}Clicked()\n\t{{\n\t}}\n";
		public const string eventCallbackSerilCode = "\n\tprivate void On{0}ValueChanged({1} value)\n\t{{\n\t}}\n";
		public const string onClickedCallbackSerilCode = "\n\tprivate void OnBtnClick(GameObject go)\n\t{\n";
		#endregion

		#region 控件查找赋值格式
		public const string assignCodeFmt = "\t\t{0} = transform.Find(\"{1}\").GetComponent<{2}>();\n";
		public const string assignGameObjectCodeFmt = "\t\t{0} = transform.Find(\"{1}\").gameObject;\n";
		//根物体上挂载的控件
		public const string assignRootCodeFmt = "\t\t{0} = transform.GetComponent<{1}>();\n";
		#endregion

		#region 查找初始化代码格式
		public const string stateTransform = "\tprivate Transform transform;\n";
		public const string stateCodePrivateFmt = "\tprivate {0} {1};\n";
		public const string stateCodePublicFmt = "\tpublic {0} {1};\n";
		public const string assignTransform = "\t\t//assign transform by your ui framework\n\t\t//transform = ;\n";
		#endregion

		#region 其他结构型代码
		public const string viewholderSetEmpty = "\n\tpublic void SetEmpty()\n\t{\n\t}\n";
		public const string baseviewContextClass = "\npublic class {0}Context : BaseContext\n{{\n\t//Please add a GameViewType manually for the error below...\n\tpublic {0}Context() : base(GameViewType.{0}) {{ }}\n}}\n";
		public const string baseviewStart = "\n\tprotected override void Start()\n\t{\n\t\tbase.Start();\n\t\t//You can apply this following code or delete it.\n\t\t//InitData();\n\t\t//InitViews();\n\t\t//InitEvents();\n\t}\n";
		public const string baseviewOnEnter = "\n\tpublic override void OnEnter(BaseContext currentContext)\n\t{\n\t\tbase.Show();\n\t}\n";
		public const string baseviewOnExit = "\n\tpublic override void OnExit(BaseContext currentContext)\n\t{\n\t\tbase.Hide();\n\t}\n";
		public const string baseviewOnPause = "\n\tpublic override void OnPause(BaseContext currentContext)\n\t{\n\t\tbase.Hide();\n\t}\n";
		public const string baseviewOnResume = "\n\tpublic override void OnResume(BaseContext lattContext)\n\t{\n\t\tbase.Show();\n\t}\n";
		#endregion

		#endregion
	}
}
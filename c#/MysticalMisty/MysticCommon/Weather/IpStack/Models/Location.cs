﻿/**********************************************************************
	Copyright 2021 Misty Robotics
	Licensed under the Apache License, Version 2.0 (the "License");
	you may not use this file except in compliance with the License.
	You may obtain a copy of the License at
		http://www.apache.org/licenses/LICENSE-2.0
	Unless required by applicable law or agreed to in writing, software
	distributed under the License is distributed on an "AS IS" BASIS,
	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	See the License for the specific language governing permissions and
	limitations under the License.
	**WARRANTY DISCLAIMER.**
	* General. TO THE MAXIMUM EXTENT PERMITTED BY APPLICABLE LAW, MISTY
	ROBOTICS PROVIDES THIS SAMPLE SOFTWARE "AS-IS" AND DISCLAIMS ALL
	WARRANTIES AND CONDITIONS, WHETHER EXPRESS, IMPLIED, OR STATUTORY,
	INCLUDING THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
	PURPOSE, TITLE, QUIET ENJOYMENT, ACCURACY, AND NON-INFRINGEMENT OF
	THIRD-PARTY RIGHTS. MISTY ROBOTICS DOES NOT GUARANTEE ANY SPECIFIC
	RESULTS FROM THE USE OF THIS SAMPLE SOFTWARE. MISTY ROBOTICS MAKES NO
	WARRANTY THAT THIS SAMPLE SOFTWARE WILL BE UNINTERRUPTED, FREE OF VIRUSES
	OR OTHER HARMFUL CODE, TIMELY, SECURE, OR ERROR-FREE.
	* Use at Your Own Risk. YOU USE THIS SAMPLE SOFTWARE AND THE PRODUCT AT
	YOUR OWN DISCRETION AND RISK. YOU WILL BE SOLELY RESPONSIBLE FOR (AND MISTY
	ROBOTICS DISCLAIMS) ANY AND ALL LOSS, LIABILITY, OR DAMAGES, INCLUDING TO
	ANY HOME, PERSONAL ITEMS, PRODUCT, OTHER PERIPHERALS CONNECTED TO THE PRODUCT,
	COMPUTER, AND MOBILE DEVICE, RESULTING FROM YOUR USE OF THIS SAMPLE SOFTWARE
	OR PRODUCT.
	Please refer to the Misty Robotics End User License Agreement for further
	information and full details:
		https://www.mistyrobotics.com/legal/end-user-license-agreement/
**********************************************************************/

using System.Collections.Generic;

namespace Conversation.Weather.OpenWeather.IpStack.Models
{
	public class Location
	{
		/// <summary>
		/// Returns the unique geoname identifier in accordance with the Geonames Registry.
		/// </summary>
		public string GeonameId { get; set; }

		/// <summary>
		/// Returns the capital city of the country associated with the IP.
		/// </summary>
		public string Capital { get; set; }

		/// <summary>
		/// Returns an object containing one or multiple sub-objects per language spoken in the country associated with the IP.
		/// </summary>
		public List<Language> Languages { get; set; }

		/// <summary>
		/// Returns an HTTP URL leading to an SVG-flag icon for the country associated with the IP.
		/// </summary>
		public string CountryFlag { get; set; }

		/// <summary>
		/// Returns the emoji icon for the flag of the country associated with the IP.
		/// </summary>
		public string CountryFlagEmoji { get; set; }

		/// <summary>
		/// Returns the unicode value of the emoji icon for the flag of the country associated with the IP.
		/// </summary>
		public string CountryFlagEmojiUnicode { get; set; }

		/// <summary>
		/// Returns the calling/dial code of the country associated with the IP.
		/// </summary>
		public string CallingCode { get; set; }

		/// <summary>
		/// Returns true or false depending on whether or not the county associated with the IP is in the European Union.
		/// </summary>
		public bool IsEu { get; set; }
	}
}

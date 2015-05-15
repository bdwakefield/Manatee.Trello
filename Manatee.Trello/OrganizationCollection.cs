/***************************************************************************************

	Copyright 2015 Greg Dennis

	   Licensed under the Apache License, Version 2.0 (the "License");
	   you may not use this file except in compliance with the License.
	   You may obtain a copy of the License at

		 http://www.apache.org/licenses/LICENSE-2.0

	   Unless required by applicable law or agreed to in writing, software
	   distributed under the License is distributed on an "AS IS" BASIS,
	   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	   See the License for the specific language governing permissions and
	   limitations under the License.
 
	File Name:		OrganizationCollection.cs
	Namespace:		Manatee.Trello
	Class Name:		ReadOnlyOrganizationCollection, OrganizationCollection
	Purpose:		Collection objects for organizations.

***************************************************************************************/
using System.Collections.Generic;
using System.Linq;
using Manatee.Trello.Exceptions;
using Manatee.Trello.Internal;
using Manatee.Trello.Internal.Caching;
using Manatee.Trello.Internal.DataAccess;
using Manatee.Trello.Internal.Validation;
using Manatee.Trello.Json;

namespace Manatee.Trello
{
	/// <summary>
	/// A read-only collection of organizations.
	/// </summary>
	public class ReadOnlyOrganizationCollection : ReadOnlyCollection<Organization>
	{
		private Dictionary<string, object> _additionalParameters;

		internal ReadOnlyOrganizationCollection(string ownerId, TrelloAuthorization auth)
			: base(ownerId, auth) {}
		internal ReadOnlyOrganizationCollection(ReadOnlyOrganizationCollection source, TrelloAuthorization auth)
			: this(source.OwnerId, auth)
		{
			if (source._additionalParameters != null)
				_additionalParameters = new Dictionary<string, object>(source._additionalParameters);
		}

		/// <summary>
		/// Retrieves a organization which matches the supplied key.
		/// </summary>
		/// <param name="key">The key to match.</param>
		/// <returns>The matching organization, or null if none found.</returns>
		/// <remarks>
		/// Matches on Organization.Id, Organization.Name, and Organization.DisplayName.  Comparison is case-sensitive.
		/// </remarks>
		public Organization this[string key] { get { return GetByKey(key); } }

		/// <summary>
		/// Implement to provide data to the collection.
		/// </summary>
		protected override sealed void Update()
		{
			IncorporateLimit(_additionalParameters);

			var endpoint = EndpointFactory.Build(EntityRequestType.Member_Read_Organizations, new Dictionary<string, object> { { "_id", OwnerId } });
			var newData = JsonRepository.Execute<List<IJsonOrganization>>(Auth, endpoint, _additionalParameters);

			Items.Clear();
			Items.AddRange(newData.Select(jo =>
				{
					var org = jo.GetFromCache<Organization>(Auth);
					org.Json = jo;
					return org;
				}));
		}

		internal void SetFilter(OrganizationFilter cardStatus)
		{
			if (_additionalParameters == null)
				_additionalParameters = new Dictionary<string, object>();
			_additionalParameters["filter"] = cardStatus.GetDescription();
		}

		private Organization GetByKey(string key)
		{
			return this.FirstOrDefault(o => key.In(o.Id, o.Name, o.DisplayName));
		}
	}

	/// <summary>
	/// A collection of organizations.
	/// </summary>
	public class OrganizationCollection : ReadOnlyOrganizationCollection
	{
		internal OrganizationCollection(string ownerId, TrelloAuthorization auth)
			: base(ownerId, auth) {}

		/// <summary>
		/// Creates a new organization.
		/// </summary>
		/// <param name="name">The name of the organization to add.</param>
		/// <returns>The <see cref="Organization"/> generated by Trello.</returns>
		public Organization Add(string name)
		{
			var error = NotNullOrWhiteSpaceRule.Instance.Validate(null, name);
			if (error != null)
				throw new ValidationException<string>(name, new[] { error });

			var json = TrelloConfiguration.JsonFactory.Create<IJsonOrganization>();
			json.Name = name;

			var endpoint = EndpointFactory.Build(EntityRequestType.Member_Write_CreateOrganization);
			var newData = JsonRepository.Execute(Auth, endpoint, json);

			return new Organization(newData, Auth);
		}
	}
}
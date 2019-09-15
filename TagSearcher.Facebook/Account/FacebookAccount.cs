using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagSearcher.Facebook.Account
{
    public class FacebookAccount
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<City> Cities { get; set; }
        public List<Education> Educations { get; set; }
        public List<Work> Works { get; set; }
        public List<BasicInfo> BasicInfos { get; set; }
        public List<FamilyMember> Family { get; set; }
        public string Relationship { get; set; }
        public string About { get; set; }
        public string FavoriteQuotes { get; set; }
        public List<ContactInformation> ContactInformations { get; set; }

        //public List<Friend> Friends { get; set; }
        //public List<Follower> Followers { get; set; }

        public List<Place> Places { get; set; }
        public List<RecentItem> Recent { get; set; }
        public List<VisitedCity> VisitedCities { get; set; }

        public List<SportTeam> SportTeams { get; set; }
        public List<SportAthlete> SportAthletes { get; set; } 

        public List<MusicItem> Music { get; set; }
        public List<Movie> Movies { get; set; }
        public List<TVShow> TVShows { get; set; }
        public List<Book> Books { get; set; }
        
        public string Skills { get; set; }

        //public List<Public> Publics { get; set; }
        public List<string> Nicknames { get; set; }

        //public byte Parser_All { get; set; }
        //public DateTime Parser_All_Last_Update { get; set; }

        //public byte Parser_Friends { get; set; }
        //public DateTime Parser_Friends_Last_Update { get; set; }

        //public byte Parser_Followers { get; set; }
        //public DateTime Parser_Followers_Last_Update { get; set; }
    }
}
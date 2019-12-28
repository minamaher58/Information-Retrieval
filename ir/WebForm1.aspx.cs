using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Porter2Stemmer;

namespace WebApplication1
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        public Dictionary<String, Dictionary<int, List<int>>> TERMS;
        public List<String> queryTerms;
        public List<String> StemmedQueryTerms;
        public IRTest_Entities TE;
        List<String> DBNames;
        public Dictionary<int, int> DOCS_EXACT;
        public List<int> DOCS_RANDOM;
        public List<int> DOCS_DIS;
        List<int> NON_CANDIDATE;

        public int EditDistance(String x, String y)
        {
            int L = x.Length;
            int LL = y.Length;
            int[,] Matrix = new int[L + 2, LL + 2];
            for (int i = 1; i <= L; i++) Matrix[i, 0] = i;
            for (int i = 1; i <= LL; i++) Matrix[0, i] = i;
            for (int i = 1; i <= L; i++)
            {
                for (int j = 1; j <= LL; j++)
                {
                    int F = Matrix[i - 1, j - 1];
                    if (x[i - 1] != y[j - 1]) F++;
                    int F2 = Matrix[i - 1, j] + 1;
                    int F3 = Matrix[i, j - 1] + 1;
                    Matrix[i, j] = Math.Min(F, Math.Min(F2, F3));
                }
            }
            return Matrix[L, LL];
        }
        public string ComputeSoundex(String s)
        {
            List<char> L0 = new List<char>() { 'a', 'e', 'i', 'o', 'u', 'h', 'w', 'y' };
            List<char> L1 = new List<char>() { 'b', 'f', 'p', 'v' };
            List<char> L2 = new List<char>() { 'c','g','j','k','q','s','x','z' };
            List<char> L3 = new List<char>() { 'd', 't' };
            List<char> L4 = new List<char>() { 'l' };
            List<char> L5 = new List<char>() { 'm', 'n' };
            List<char> L6 = new List<char>() { 'r' };
            String f = "";
            f += s[0];
            for (int i = 1; i < s.Length; i++)
            {
                char ch = s[i];
                if (L0.Contains(ch)) { f += '0'; }
                else if (L1.Contains(ch)) { f += '1'; }
                else if (L2.Contains(ch)) { f += '2'; }
                else if (L3.Contains(ch)) { f += '3'; }
                else if (L4.Contains(ch)) { f += '4'; }
                else if (L5.Contains(ch)) { f += '5'; }
                else if (L6.Contains(ch)) { f += '6'; }
            }
            string f2 = "";
            f2 += f[0];
            for (int i = 1; i < f.Length; i++)
            {
                if (f[i] != f[i - 1]) f2 += f[i];
            }
            string f3 = "";
            for (int i = 0; i < f2.Length; i++)
            {
                if (f2[i] != '0') f3 += f2[i];
            }
            while (f3.Length < 4) f3 += '0';
            string f4 = "";
            for (int i = 0; i < 4; i++) { f4 += f3[i]; }
            return f4;
        }
        bool SOLVE(int index,int total, int doc, int pos)
        {
            if (index == total)
            {
                return true;
            }
            if (DBNames.Contains(StemmedQueryTerms[index]) == false)
            {
                return false;
            }
            if (index == 0)
            {
                Dictionary<int, List<int>> DC = TERMS[StemmedQueryTerms[index]];
                foreach (KeyValuePair<int,List<int>> EE in DC)
                {
                    for (int i = 0; i < EE.Value.Count; i++)
                    {
                        if (SOLVE(1, total, EE.Key, EE.Value[i] + 1) == true)
                        {
                            if (DOCS_EXACT.ContainsKey(EE.Key)==true)
                            {
                                DOCS_EXACT[EE.Key]++;
                            }
                            else
                            {
                                DOCS_EXACT.Add(EE.Key, 1);
                            }
                        }
                    }
                }
            }
            else
            {
                Dictionary<int, List<int>> DC = TERMS[StemmedQueryTerms[index]];
                if (DC.ContainsKey(doc) == false) return false;
                if (DC[doc].Contains(pos) == false) return false;
                if (SOLVE(index + 1, total, doc, pos + 1) == false) return false;
                return true;
            }
            return false;
        }
        void SOLVE_RANDOM(int COUNT)
        {
            int[] DOCS_COUNT = new int[1610];
            List<int> CANDIDATE_DOCS = new List<int>();
            foreach (KeyValuePair<String, Dictionary<int,List<int>>> EE in TERMS)
            {
                foreach (KeyValuePair<int, List<int>> EE2 in EE.Value)
                {
                    DOCS_COUNT[EE2.Key]++;
                }
            }
            for (int i = 1; i <= 1600; i++)
            {
                if (DOCS_COUNT[i] == COUNT)
                {
                    CANDIDATE_DOCS.Add(i);
                }
                else if (DOCS_COUNT[i] < COUNT && DOCS_COUNT[i] > 0)
                {
                    NON_CANDIDATE.Add(i);
                }
            }
            for (int u = 0; u < CANDIDATE_DOCS.Count; u++)
            {
                int DOC_ = CANDIDATE_DOCS[u];
                int MIN_DIS = 1000000000;
                for (int i = 0; i < COUNT-1; i++)
                {
                    for (int j = i + 1; j < COUNT; j++)
                    {
                        List<int> POS1 = TERMS[StemmedQueryTerms[i]][DOC_];
                        List<int> POS2 = TERMS[StemmedQueryTerms[j]][DOC_];
                        for (int F1 = 0; F1 < POS1.Count; F1++)
                        {
                            for (int F2 = 0; F2 < POS2.Count; F2++)
                            {
                                MIN_DIS = Math.Min(MIN_DIS, Math.Abs(POS1[F1] - POS2[F2]));
                            }
                        }
                    }
                }
                DOCS_RANDOM.Add(DOC_);
                DOCS_DIS.Add(MIN_DIS);
            }
            //sorting
            for (int write = 0; write < DOCS_DIS.Count; write++)
            {
                for (int sort = 0; sort < DOCS_DIS.Count - 1; sort++)
                {
                    if (DOCS_DIS[sort] > DOCS_DIS[sort + 1])
                    {
                        int temp = DOCS_DIS[sort + 1];
                        DOCS_DIS[sort + 1] = DOCS_DIS[sort];
                        DOCS_DIS[sort] = temp;

                        int TINT = DOCS_RANDOM[sort + 1];
                        DOCS_RANDOM[sort + 1] = DOCS_RANDOM[sort];
                        DOCS_RANDOM[sort] = TINT;
                    }
                }
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            String query = Query.Text;
            Mean.Text = "";
            Results.Items.Clear();
            NonResults.Items.Clear();
            Suggested.Items.Clear();
            Proximity.Items.Clear();
            if (query.Length == 0) return;
            queryTerms = new List<String>();
            StemmedQueryTerms = new List<String>();
            TE = new IRTest_Entities();

            bool Spelling = Spell.Checked;
            bool Soundex = Sound.Checked;
            bool ExactSearch = true;
            if (query[0] != '"') ExactSearch = false;
            
            //Retrieving Query Terms
            int Begin = 0;
            int End = query.Length;
            String Term = "";
            if (ExactSearch == true)
            {
                Begin++;
                End--;
            }
            for (int i = Begin; i < End; i++)
            {
                if (query[i] == ' ')
                {
                    if (Term.Length > 0)
                    {
                        String TTerm = "";
                        for (int j = 0; j < Term.Length; j++) TTerm += Char.ToLower(Term[j]);
                        queryTerms.Add(TTerm);
                        EnglishPorter2Stemmer stemmer = new EnglishPorter2Stemmer();
                        String STTERM = stemmer.Stem(TTerm).Value;
                        StemmedQueryTerms.Add(STTERM);
                    }
                    Term = "";
                }
                else
                {
                    Term += query[i];
                    if (i == End - 1)
                    {
                         String TTerm = "";
                         for (int j = 0; j < Term.Length; j++) TTerm += Char.ToLower(Term[j]);
                         queryTerms.Add(TTerm);
                         EnglishPorter2Stemmer stemmer = new EnglishPorter2Stemmer();
                         String STTERM = stemmer.Stem(TTerm).Value;
                         StemmedQueryTerms.Add(STTERM);
                    }
                }
            }

            //Spelling Correction
            if (Spelling == true)
            {
                String CorrectedWords = "";
                for (int i = 0; i < queryTerms.Count; i++)
                {
                    List<String> SimilarWords = new List<String>();
                    String Tquery = "$";
                    Tquery += queryTerms[i];
                    Tquery += "$";
                    for (int j = 0; j < Tquery.Length-1; j++)
                    {
                        String Gram = Tquery.Substring(j, 2);
                        List<String> CandidateTerms = new List<String>();
                        foreach (Bigram BG in TE.Bigrams)
                        {
                            if (Gram .Equals(BG.gram))
                            {
                                String DictionaryTerms = BG.terms;
                                String Temp = "";
                                for (int u = 0; u < DictionaryTerms.Length; u++)
                                {
                                    if (DictionaryTerms[u] == ' ')
                                    {
                                        if (Temp.Length > 0) 
                                        {
                                            String TTemp = "$";
                                            TTemp += Temp;
                                            TTemp += "$";
                                            CandidateTerms.Add(TTemp);
                                        }
                                        Temp = "";
                                    }
                                    else
                                    {
                                        Temp += DictionaryTerms[u];
                                        if (u == DictionaryTerms.Length - 1)
                                        {
                                             String TTemp = "$";
                                             TTemp += Temp;
                                             TTemp += "$";
                                             CandidateTerms.Add(TTemp);
                                        }
                                    }
                                }
                                break;
                            }
                        }
                        for (int u = 0; u < CandidateTerms.Count; u++)
                        {
                            String Candidate = CandidateTerms[u];
                            double CommonGrams = 0;
                            double QueryGrams = Tquery.Length - 1;
                            double TermGrams = Candidate.Length - 1;
                            for (int f = 0; f < Tquery.Length - 1; f++)
                            {
                                for (int ff = 0; ff < Candidate.Length - 1; ff++)
                                {
                                    if (Tquery.Substring(f, 2) .Equals(Candidate.Substring(ff, 2)))
                                    {
                                        CommonGrams++;
                                        break;
                                    }
                                }
                            }
                            double Jaccard = (2.0 * CommonGrams) / (QueryGrams + TermGrams);
                            Jaccard *= 100.0;
                            if (Jaccard >= 45)
                            {
                                if (!(SimilarWords.Contains(Candidate))) SimilarWords.Add(Candidate);
                            }
                        }
                    }
                    List<int> Distances = new List<int>();
                    for (int f = 0; f < SimilarWords.Count; f++)
                    {
                        String Temp = "";
                        for (int ff = 1; ff < SimilarWords[f].Length-1; ff++)
                        {
                            Temp += SimilarWords[f][ff];
                        }
                        int _EditDistance = EditDistance(queryTerms[i], Temp);
                        Distances.Add(_EditDistance);
                    }
                    //Sorting
                    for (int write = 0; write < SimilarWords.Count; write++)
                    {
                        for (int sort = 0; sort < SimilarWords.Count - 1; sort++)
                        {
                            if (Distances[sort] > Distances[sort + 1])
                            {
                                int temp = Distances[sort + 1];
                                Distances[sort + 1] = Distances[sort];
                                Distances[sort] = temp;

                                String TSTR = SimilarWords[sort + 1];
                                SimilarWords[sort + 1] = SimilarWords[sort];
                                SimilarWords[sort] = TSTR;
                            }
                        }
                    }
                    if (SimilarWords.Count > 0) CorrectedWords += SimilarWords[0].Substring(1,SimilarWords[0].Length-2);
                    else CorrectedWords += "NULL";
                    if(i<queryTerms.Count-1)CorrectedWords += ' ';
                    for(int f = 0;f < SimilarWords.Count;f++)
                    {
                        Suggested.Items.Add(SimilarWords[f].Substring(1,SimilarWords[f].Length-2));
                    }
                }
                Mean.Text = CorrectedWords;
            }

            //Soundex 
            if (Soundex == true)
            {
                List<int> DOCS = new List<int>();
                List<int> FREQS = new List<int>();
                String Code = ComputeSoundex(queryTerms[0]);
                foreach (SoundCode SC in TE.SoundCodes)
                {
                    if(Code.Equals(SC.code))
                    {
                        String PTerms = SC.terms;
                        String Temp2 = "";
                        for(int i = 0;i < PTerms.Length;i++)
                        {
                            if(PTerms[i]==' ')
                            {
                                if (Temp2.Length > 0)
                                {
                                    EnglishPorter2Stemmer stemmer = new EnglishPorter2Stemmer();
                                    String STEMMED = stemmer.Stem(Temp2).Value;
                                    foreach (II_Stemming II in TE.II_Stemming)
                                    {
                                        if (STEMMED == II.name)
                                        {
                                            int _docid = (int)II.docid;
                                            int _frequency = (int)II.frequency;
                                            DOCS.Add(_docid);
                                            FREQS.Add(_frequency);
                                        }
                                    }
                                }
                                Temp2 = "";
                            }
                            else
                            {
                                Temp2 += PTerms[i];
                                if(i==PTerms.Length-1)
                                {
                                    EnglishPorter2Stemmer stemmer = new EnglishPorter2Stemmer();
                                    String STEMMED = stemmer.Stem(Temp2).Value;
                                    foreach (II_Stemming II in TE.II_Stemming)
                                    {
                                        if (STEMMED == II.name)
                                        {
                                            int _docid = (int)II.docid;
                                            int _frequency = (int)II.frequency;
                                            DOCS.Add(_docid);
                                            FREQS.Add(_frequency);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
                //Sorting
                for (int write = 0; write < FREQS.Count; write++)
                {
                    for (int sort = 0; sort < FREQS.Count - 1; sort++)
                    {
                        if (FREQS[sort] < FREQS[sort + 1])
                        {
                            int temp = FREQS[sort + 1];
                            FREQS[sort + 1] = FREQS[sort];
                            FREQS[sort] = temp;

                            int TINT = DOCS[sort + 1];
                            DOCS[sort + 1] = DOCS[sort];
                            DOCS[sort] = TINT;
                        }
                    }
                }
                List<String> URLS = new List<String>();
                foreach (EnglishPage EP in TE.EnglishPages.ToList())
                {
                    URLS.Add(EP.URL);
                }
                for (int i = 0; i < DOCS.Count; i++)
                {
                    Results.Items.Add(URLS[DOCS[i] - 1]);
                    Proximity.Items.Add(FREQS[i].ToString());
                }
                //for (int i = 0; i < SimilarWords.Count; i++) Results.Items.Add(SimilarWords[i]);
            }
            else if (queryTerms.Count > 0)
            {
                TERMS = new Dictionary<String, Dictionary<int, List<int>>>();
                DBNames = new List<String>();
                List<String> URLS = new List<String>();
                foreach (EnglishPage EP in TE.EnglishPages.ToList())
                {
                    URLS.Add(EP.URL);
                }
                foreach (II_Stemming II in TE.II_Stemming.ToList())
                {
                    DBNames.Add(II.name);
                    if (StemmedQueryTerms.Contains(II.name) == true)
                    {
                        if (TERMS.ContainsKey(II.name) == true)
                        {
                            if (TERMS[II.name].ContainsKey((int)II.docid) == false)
                            {
                                String POS = II.positions;
                                int C = 0;
                                List<int> POSS = new List<int>();
                                for (int i = 1; i < POS.Length; i++)
                                {
                                    if (POS[i] == ' ')
                                    {
                                        POSS.Add(C);
                                        C = 0;
                                    }
                                    else
                                    {
                                        C = (C * 10) + ((int)(POS[i] - '0'));
                                        if (i == POS.Length - 1) POSS.Add(C);
                                    }
                                }
                                TERMS[II.name].Add((int)II.docid, POSS);
                            }
                        }
                        else
                        {
                            TERMS.Add(II.name, new Dictionary<int, List<int>>());
                            String POS = II.positions;
                            int C = 0;
                            List<int> POSS = new List<int>();
                            for (int i = 1; i < POS.Length; i++)
                            {
                                if (POS[i] == ' ')
                                {
                                    POSS.Add(C);
                                    C = 0;
                                }
                                else
                                {
                                    C = (C * 10) + ((int)(POS[i] - '0'));
                                    if (i == POS.Length - 1) POSS.Add(C);
                                }
                            }
                            TERMS[II.name].Add((int)II.docid, POSS);
                        }
                    }
                }
                if (ExactSearch == true)
                {
                    DOCS_EXACT = new Dictionary<int, int>();
                    int[] DOCS_COUNT = new int[1610];
                    List<int> NON_CANDIDATE_DOCS = new List<int>();
                    foreach (KeyValuePair<String, Dictionary<int, List<int>>> EE in TERMS)
                    {
                        foreach (KeyValuePair<int, List<int>> EE2 in EE.Value)
                        {
                            DOCS_COUNT[EE2.Key]++;
                        }
                    }
                    for (int i = 1; i <= 1600; i++)
                    {
                        if (DOCS_COUNT[i] < StemmedQueryTerms.Count&&DOCS_COUNT[i] > 0)
                        {
                            NON_CANDIDATE_DOCS.Add(i);
                        }
                    }
                    bool TR = SOLVE(0, queryTerms.Count, 0, 0);
                    List<int> DOCS_ = new List<int>();
                    List<int> Freqs_ = new List<int>();
                    foreach (KeyValuePair<int, int> EE in DOCS_EXACT)
                    {
                        DOCS_.Add(EE.Key);
                        Freqs_.Add(EE.Value);
                    }
                    //sorting
                    for (int write = 0; write < Freqs_.Count; write++)
                    {
                        for (int sort = 0; sort < Freqs_.Count - 1; sort++)
                        {
                            if (Freqs_[sort] < Freqs_[sort + 1])
                            {
                                int temp = Freqs_[sort + 1];
                                Freqs_[sort + 1] = Freqs_[sort];
                                Freqs_[sort] = temp;

                                int TINT = DOCS_[sort + 1];
                                DOCS_[sort + 1] = DOCS_[sort];
                                DOCS_[sort] = TINT;
                            }
                        }
                    }
                    for (int i = 0; i < DOCS_.Count; i++)
                    {
                        Results.Items.Add(URLS[DOCS_[i] - 1]);
                        Proximity.Items.Add(Freqs_[i].ToString());
                    }
                    //NON COMMON RESULTS
                    for (int i = 0; i < NON_CANDIDATE_DOCS.Count; i++)
                    {
                        NonResults.Items.Add(URLS[NON_CANDIDATE_DOCS[i] - 1]);
                    }
                }
                else
                {
                    DOCS_RANDOM = new List<int>();
                    DOCS_DIS = new List<int>();
                    NON_CANDIDATE = new List<int>();
                    SOLVE_RANDOM(queryTerms.Count);
                    for (int i = 0; i < DOCS_RANDOM.Count; i++)
                    {
                        Results.Items.Add(URLS[DOCS_RANDOM[i]-1]);
                        Proximity.Items.Add(DOCS_DIS[i].ToString());
                    }
                    //NON COMMON RESULTS
                    for (int i = 0; i < NON_CANDIDATE.Count; i++)
                    {
                        NonResults.Items.Add(URLS[NON_CANDIDATE[i] - 1]);
                    }
                }
            }
        }
    }
}
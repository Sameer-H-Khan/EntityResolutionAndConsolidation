import pandas as pd
import numpy as np
from sentence_transformers import SentenceTransformer, util
from sklearn.metrics.pairwise import cosine_similarity
from sklearn.cluster import DBSCAN, AgglomerativeClustering


def main():
    df = pd.read_csv('book.tsv', sep='\t', header=None)
    source = df.iloc[:, 3]
    source = source.dropna()
    # authors = source[145:165]
    # authors = pd.concat([authors, source[420:440]]) 
    authors = source[:5000]

    model = SentenceTransformer('paraphrase-MiniLM-L6-v2')
    author_embeddings = [model.encode(author) for author in authors]

    print('\n#######################\n')

    # Compute pairwise cosine similarity
    cosine_similarities = cosine_similarity(author_embeddings)
    print(cosine_similarities.shape)

    # Perform hierarchical clustering
    # You may need to adjust parameters such as linkage and distance_threshold based on your data
    # clustering = AgglomerativeClustering(n_clusters=None, linkage='average', distance_threshold=0.6).fit(cosine_similarities)
    clustering = AgglomerativeClustering(n_clusters=None, linkage='average', distance_threshold=0.3).fit(cosine_similarities)

    # Get cluster labels
    cluster_labels = clustering.labels_

    # Group authors based on cluster labels
    clustered_authors = {}
    for i, label in enumerate(cluster_labels):
        if label not in clustered_authors:
            clustered_authors[label] = []
        clustered_authors[label].append(authors.iloc[i])

    # Print authors in each cluster
    for cluster, authors in clustered_authors.items():
        print(f'Cluster {cluster}:')
        print(authors)


if __name__ == "__main__":
    main()